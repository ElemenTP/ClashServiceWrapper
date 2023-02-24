using System.ComponentModel;
using System.IO.Pipes;
using System.ServiceProcess;

namespace ClashServiceWrapper
{
    internal sealed class ClientController
    {
        private readonly ServiceController svc;
        private readonly NamedPipeServerStream pipeServerStream;
        private readonly ManualResetEventSlim stopEvent;
        private readonly string pipeName;
        private volatile bool isExpected;

        public ClientController()
        {
            svc = new(Constant.serviceName);
            pipeName = Guid.NewGuid().ToString();
            pipeServerStream = new(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            stopEvent = new();
            isExpected = false;
            ConsoleApis.SetConsoleOutputCP(ConsoleApis.CP_UTF8);
        }

        public void StartService(string args, bool mon)
        {
            ConsoleApis.SetConsoleCtrlHandler(ConsoleCtrlHandler, true);
            if (mon)
            {
                Task.Run(StartPipeServer);
            }
            string[] svcargs = new string[3];
            svcargs[0] = pipeName;
            svcargs[1] = args;
            svcargs[2] = mon ? "true" : "false";
            StartServiceInner(svcargs);
            if (mon)
            {
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
        }

        private bool ConsoleCtrlHandler(ConsoleApis.CtrlEvents _)
        {
            isExpected = true;
            stopEvent.Set();
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
                stopEvent.Set();
            }
        }

        private void StartPipeServer()
        {
            pipeServerStream.WaitForConnection();
            try
            {
                pipeServerStream.CopyTo(Console.OpenStandardOutput());
            }
            finally
            {
                stopEvent.Set();
            }
        }

        private void StopPipeServer()
        {
            try
            {
                if (pipeServerStream.IsConnected)
                {
                    pipeServerStream.Disconnect();
                }
            }
            finally
            {
                pipeServerStream.Close();
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
                svc.Dispose();
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
