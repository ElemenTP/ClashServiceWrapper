using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using WinSW.Native;
using static WinSW.Native.ConsoleApis;

namespace ClashSvcClient
{
    internal sealed class ControllerService
    {
        private readonly ServiceController svc;
        private readonly NamedPipeServerStream pipeServerStream;
        private readonly ManualResetEventSlim stopEvent;
        private readonly string pipeName;
        private volatile bool isExpected;

        public ControllerService()
        {
            svc = new(Constant.serviceName);
            pipeName = Guid.NewGuid().ToString();
            pipeServerStream = new(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
            stopEvent = new();
            isExpected = false;
            SetConsoleOutputCP(CP_UTF8);
        }

        public void Run()
        {
            SetConsoleCtrlHandler(ConsoleCtrlHandler, true);
            Task.Run(StartPipeServer);
            string[] svcargs = new string[2];
            svcargs[0] = pipeName;
            {
                string cmdline = Marshal.PtrToStringUni(GetCommandLineW())!;
                bool notQuoted = true;
                for (int i = 0; i < cmdline.Length; i++)
                {
                    switch (cmdline[i])
                    {
                        case '"':
                            notQuoted = !notQuoted;
                            break;
                        case ' ':
                            if (notQuoted && i < cmdline.Length - 1)
                            {
                                svcargs[1] = cmdline[(i + 1)..];
                                i = cmdline.Length;
                            }
                            break;
                    }
                }
                if (svcargs[1] == null)
                {
                    svcargs[1] = "-d \"" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.config\\clash\"";
                }
            }
            StartService(svcargs);
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

        private bool ConsoleCtrlHandler(CtrlEvents _)
        {
            isExpected = true;
            stopEvent.Set();
            return true;
        }

        private void CheckHealth()
        {
            try
            {
                using Mutex mutex = MutexAcl.OpenExisting("Global\\ClashServiceHost", MutexRights.Delete | MutexRights.Modify | MutexRights.Synchronize | MutexRights.TakeOwnership);
                mutex.WaitOne();
            }
            catch { }
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
            catch { }
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
            catch { }
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
    }
}
