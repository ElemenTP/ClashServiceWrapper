using System.Diagnostics;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Text;
using WinSW.Native;
namespace ClashSvcHost
{
    public sealed class WrapperService : ServiceBase
    {
        private Process? process;
        private volatile NamedPipeClientStream? pipeClientStream;
        private volatile bool notExpected = false;

        public WrapperService()
        {
            ServiceName = Constant.serviceName;
            CanStop = true;
            CanShutdown = true;
            CanPauseAndContinue = false;
            AutoLog = false;
            ConsoleApis.SetConsoleOutputCP(ConsoleApis.CP_UTF8);
        }

        protected override void OnStart(string[] args)
        {
            if (args.Length == 0)
            {
                Stop();
                return;
            }
            try
            {
                pipeClientStream = new(".", args[0], PipeDirection.Out, PipeOptions.WriteThrough);
                pipeClientStream.Connect(500);
            }
            catch (Exception)
            {
                Stop();
                return;
            }
            Task checkHealthTask = new(CheckHealth);
            checkHealthTask.Start();
            try
            {
                ProcessStartInfo info = new()
                {
                    FileName = "\"" + Constant.exeDir + Constant.clashName + "\"",
                    Arguments = "\"" + string.Join("\" \"", args[1..]) + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ErrorDialog = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.UTF8,
                };
                process = Process.Start(info);
                if (process == null) { throw new Exception(); }
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
                process.Exited += (_, _) =>
                {
                    notExpected = true;
                    Stop();
                };
                process.EnableRaisingEvents = true;
                process.StandardOutput.BaseStream.CopyToAsync(pipeClientStream);
                process.StandardError.BaseStream.CopyToAsync(pipeClientStream);
            }
            catch (Exception e)
            {
                try
                {
                    byte[] b = Encoding.UTF8.GetBytes(e.Message + "\n");
                    pipeClientStream!.Write(b);
                }
                catch (Exception) { }
                Stop();
                return;
            }
        }

        private void CheckHealth()
        {
            try
            {
                using Mutex mutex = new(false, "Global\\ClashServiceClient");
                mutex.WaitOne();
            }
            catch (Exception) { }
            finally
            {
                Stop();
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
                    catch (Exception) { }
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
                    catch (Exception) { }
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
