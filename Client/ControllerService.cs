using System.ComponentModel;
using System.IO.Pipes;
using System.ServiceProcess;
using WinSW.Native;
namespace ClashSvcClient
{
    internal class ControllerService
    {
        private readonly ServiceController svc;
        private readonly NamedPipeServerStream pipeServerStream;
        private readonly ManualResetEventSlim stopEvent;
        private readonly string pipeName;
        private volatile bool isExpected;

        public ControllerService()
        {
            svc = new(Constant.serviceName);
            pipeName = "LOCAL\\{" + Guid.NewGuid().ToString() + "}";
            pipeServerStream = new(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.WriteThrough);
            stopEvent = new();
            isExpected = false;
            ConsoleApis.SetConsoleOutputCP(ConsoleApis.CP_UTF8);
        }

        public void Run(string[] args)
        {
            ConsoleApis.SetConsoleCtrlHandler(ConsoleCtrlHandler, true);
            Task pipeRecvTask = new(StartPipeServer);
            pipeRecvTask.Start();
            string[] svcargs = new string[args.Length + 1];
            svcargs[0] = pipeName;
            for (int i = 0; i < args.Length; i++)
            {
                svcargs[i + 1] = args[i];
            }
            StartService(svcargs);
            Task checkHealthTask = new(CheckHealth);
            checkHealthTask.Start();
            stopEvent.Wait();
            if (!isExpected)
            {
                Console.WriteLine("Service stopped unexpectedly, existing...");
            }
            if (svc.Status == ServiceControllerStatus.Running)
            {
                StopService();
            }
            if (pipeServerStream.IsConnected == true)
            {
                StopPipeServer();
            }
            stopEvent.Dispose();
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
                using Mutex mutex = new(false, "Global\\ClashServiceHost");
                mutex.WaitOne();
            }
            catch (Exception) { }
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
            catch (Exception) { }
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
            catch (Exception) { }
            finally
            {
                pipeServerStream.Close();
                pipeServerStream.Dispose();
            }
        }

        private void StartService(string[] args)
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
                    Throw.Command.Exception("The service is running!");
                }
            }
            catch (InvalidOperationException e)
            when (e.InnerException is Win32Exception inner)
            {
                Throw.Command.Exception(inner);
            }
        }

        private void StopService()
        {
            try
            {
                ServiceControllerStatus status = svc.Status;
                if (status != ServiceControllerStatus.Stopped && status != ServiceControllerStatus.StopPending)
                {
                    svc.Stop();
                    svc.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
            catch (Exception) { }
            finally
            {
                svc.Close();
                svc.Dispose();
            }
        }
    }
}
