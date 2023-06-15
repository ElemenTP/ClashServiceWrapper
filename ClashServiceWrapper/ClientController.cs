using System.ComponentModel;
using System.IO.Pipes;
using System.ServiceProcess;

namespace ClashServiceWrapper
{
    internal sealed class ClientController
    {
        private readonly ServiceController svc;
        private NamedPipeServerStream? pipeServerStream;
        private ManualResetEventSlim? stopEvent;
        private bool isExpected;

        public ClientController()
        {
            svc = new(Constant.serviceName);
            isExpected = false;
            ConsoleApis.SetConsoleOutputCP(ConsoleApis.CP_UTF8);
        }

        public void StartService(string args)
        {
            ConsoleApis.SetConsoleCtrlHandler(ConsoleCtrlHandler, true);
            string pipeName = Guid.NewGuid().ToString();
            pipeServerStream = new(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            stopEvent = new();
            Task.Run(StartPipeServer);
            string[] svcargs = new string[2];
            svcargs[0] = args;
            svcargs[1] = pipeName;
            StartServiceInner(svcargs);
            Task.Run(CheckHealth);
            stopEvent.Wait();
            if (!isExpected)
            {
                Console.WriteLine("WARNING: Service stopped unexpectedly, existing...");
            }
            if (svc.Status == ServiceControllerStatus.Running)
            {
                StopService();
            }
            if (pipeServerStream.IsConnected == true)
            {
                StopPipeServer();
            }
        }

        public void StartServiceNoMon(string args)
        {
            string[] svcargs = new string[1];
            svcargs[0] = args;
            StartServiceInner(svcargs);
        }

        private bool ConsoleCtrlHandler(ConsoleApis.CtrlEvents _)
        {
            isExpected = true;
            stopEvent!.Set();
            return true;
        }

        private void CheckHealth()
        {
            try
            {
                SingleInstance.ClientWaitForHost();
            }
            finally
            {
                stopEvent!.Set();
            }
        }

        private void StartPipeServer()
        {
            pipeServerStream!.WaitForConnection();
            try
            {
                pipeServerStream.CopyTo(Console.OpenStandardOutput());
            }
            finally
            {
                stopEvent!.Set();
            }
        }

        private void StopPipeServer()
        {
            try
            {
                if (pipeServerStream!.IsConnected)
                {
                    pipeServerStream!.Disconnect();
                }
            }
            finally
            {
                pipeServerStream!.Close();
            }
        }

        private void StartServiceInner(string[] args)
        {
            try
            {
                if (svc.Status == ServiceControllerStatus.Stopped)
                {
                    svc.Start(args);
                    svc.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(TimeSpan.TicksPerSecond));
                }
                else
                {
                    Throw.Command.Exception("The hosting service is already running.");
                }
            }
            catch (InvalidOperationException e)
            when (e.InnerException is Win32Exception inner)
            {
                Throw.Command.Exception(inner);
            }
        }

        public void StopService()
        {
            try
            {
                var status = svc.Status;
                if (status != ServiceControllerStatus.Stopped && status != ServiceControllerStatus.StopPending)
                {
                    svc.Stop();
                    svc.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
            catch (InvalidOperationException e)
            when (e.InnerException is Win32Exception inner)
            {
                Throw.Command.Exception(inner);
            }
            finally
            {
                svc.Close();
            }
        }

        public ServiceControllerStatus? QueryService()
        {
            try
            {
                return svc.Status;
            }
            catch (InvalidOperationException e)
            when (e.InnerException is Win32Exception inner)
            {
                Throw.Command.Exception(inner);
            }
            return null;
        }
    }
}
