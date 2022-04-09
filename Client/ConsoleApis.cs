using System.Runtime.InteropServices;

namespace WinSW.Native
{
    internal static class ConsoleApis
    {
        internal const uint CP_UTF8 = 65001;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetCommandLineW();

        [DllImport("kernel32.dll")]
        internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine? handlerRoutine, bool add);

        [DllImport("kernel32.dll")]
        internal static extern bool SetConsoleOutputCP(uint codePageID);

        internal delegate bool ConsoleCtrlHandlerRoutine(CtrlEvents ctrlType);

        internal enum CtrlEvents : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6,
        }
    }
}
