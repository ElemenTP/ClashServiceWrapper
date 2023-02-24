using System.Diagnostics;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Text;

namespace ClashServiceWrapper
{
    public sealed class WrapperService : ServiceBase
    {
        private Process? process;
        private volatile NamedPipeClientStream? pipeClientStream;
        private readonly ManualResetEventSlim stopEvent;
        private volatile bool stopTriggered = false;
        private volatile bool notExpected = false;

        public WrapperService()
        {
            ServiceName = Constant.serviceName;
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = false;
            AutoLog = false;
            stopEvent = new();
        }

        protected override void OnStart(string[] args)
        {
            if (args.Length != 2)
            {
                Stop();
                return;
            }
            try
            {
                pipeClientStream = new(".", args[0], PipeDirection.Out, PipeOptions.WriteThrough | PipeOptions.Asynchronous);
                pipeClientStream.Connect(500);
            }
            catch
            {
                Stop();
                return;
            }
            Task.Run(() =>
            {
                stopEvent.Wait();
                if (!stopTriggered)
                {
                    Stop();
                }
            });
            Task.Run(CheckHealth);
            try
            {
                ProcessStartInfo info = new()
                {
                    FileName = "\"" + Constant.exeDir + Constant.clashName + "\"",
                    Arguments = args[1],
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ErrorDialog = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.UTF8,
                };
                ConsoleApis.AllocConsole();
                ConsoleApis.SetConsoleOutputCP(ConsoleApis.CP_UTF8);
                process = Process.Start(info);
                ConsoleApis.FreeConsole();
                if (process == null) { throw new Exception(); }
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
                process.Exited += (_, _) =>
                {
                    notExpected = true;
                    try
                    {
                        stopEvent.Set();
                    }
                    catch { }
                };
                process.EnableRaisingEvents = true;
                Task.Run(() =>
                {
                    try
                    {
                        process.StandardOutput.BaseStream.CopyTo(pipeClientStream);
                    }
                    catch { }
                    finally
                    {
                        stopEvent.Set();
                    }
                });
                Task.Run(() =>
                {
                    try
                    {
                        process.StandardError.BaseStream.CopyTo(pipeClientStream);
                    }
                    catch { }
                    finally
                    {
                        stopEvent.Set();
                    }
                });
            }
            catch (Exception e)
            {
                try
                {
                    byte[] b = Encoding.UTF8.GetBytes(e.Message + "\n");
                    pipeClientStream!.Write(b);
                }
                catch { }
                Stop();
                return;
            }
        }

        private void CheckHealth()
        {
            try
            {
                using Mutex mutex = Mutex.OpenExisting("Global\\ClashServiceClient");
                mutex.WaitOne();
            }
            catch { }
            finally
            {
                stopEvent.Set();
            }
        }

        private void StopProcess()
        {
            process!.EnableRaisingEvents = false;
            ConsoleApis.AttachConsole(process!.Id);
            ConsoleApis.SetConsoleCtrlHandler(null, true);
            ConsoleApis.GenerateConsoleCtrlEvent(ConsoleApis.CtrlEvents.CTRL_C_EVENT, 0);
            ConsoleApis.FreeConsole();
            bool res = process!.WaitForExit(5000);
            if (!res)
            {
                process!.Kill();
            }
        }

        private void DoStop()
        {
            stopTriggered = true;
            if (process != null)
            {
                if (!process.HasExited)
                {
                    StopProcess();
                }
                else if (notExpected && pipeClientStream!.IsConnected)
                {
                    try
                    {
                        byte[] b = Encoding.UTF8.GetBytes($"Clash process existed unexpectedly, exit code: {process.ExitCode}\n");
                        pipeClientStream!.Write(b);
                    }
                    catch { }
                }
                process.Close();
                process.Dispose();
            }
            if (pipeClientStream != null)
            {
                if (pipeClientStream.IsConnected)
                {
                    try
                    {
                        pipeClientStream.Flush();
                        pipeClientStream.WaitForPipeDrain();
                    }
                    catch { }
                }
                pipeClientStream.Close();
                pipeClientStream.Dispose();
            }
        }

        protected override void OnStop()
        {
            DoStop();
        }

        protected override void OnShutdown()
        {
            DoStop();
        }
    }
}
