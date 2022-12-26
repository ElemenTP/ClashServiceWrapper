using System.Runtime.InteropServices;

namespace WinSW.Native
{
    internal static partial class ConsoleApis
    {
        internal const uint CP_UTF8 = 65001;

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr GetCommandLineW();

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine? handlerRoutine, [MarshalAs(UnmanagedType.Bool)] bool add);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetConsoleOutputCP(uint codePageID);

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
