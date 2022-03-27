using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Text;

namespace WinSW.Native
{
    internal static class ServiceApis
    {
        private const string Advapi32 = "advapi32.dll";

        [DllImport(Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "ChangeServiceConfig2W")]
        internal static extern bool ChangeServiceConfig2(IntPtr serviceHandle, ServiceConfigInfoLevels infoLevel, in SERVICE_DESCRIPTION info);

        [DllImport(Advapi32)]
        internal static extern bool CloseServiceHandle(IntPtr objectHandle);

        [DllImport(Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateServiceW")]
        internal static extern IntPtr CreateService(
            IntPtr databaseHandle,
            string serviceName,
            string displayName,
            ServiceAccess desiredAccess,
            ServiceType serviceType,
            ServiceStartMode startType,
            ServiceErrorControl errorControl,
            string binaryPath,
            string? loadOrderGroup,
            IntPtr tagId,
            StringBuilder? dependencies, // TODO
            string? serviceStartName,
            string? password);

        [DllImport(Advapi32, SetLastError = true)]
        internal static extern bool DeleteService(IntPtr serviceHandle);

        [DllImport(Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "OpenSCManagerW")]
        internal static extern IntPtr OpenSCManager(string? machineName, string? databaseName, ServiceManagerAccess desiredAccess);

        [DllImport(Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "OpenServiceW")]
        internal static extern IntPtr OpenService(IntPtr databaseHandle, string serviceName, ServiceAccess desiredAccess);

        [DllImport(Advapi32, SetLastError = true)]
        internal static extern bool QueryServiceObjectSecurity(IntPtr serviceHandle, SecurityInfos secInfo, byte[] lpSecDesrBuf, uint bufSize, out uint bufSizeNeeded);

        [DllImport(Advapi32, SetLastError = true)]
        internal static extern bool SetServiceObjectSecurity(IntPtr serviceHandle, SecurityInfos securityInformation, byte[] securityDescriptor);

        // SERVICE_
        // https://docs.microsoft.com/windows/win32/services/service-security-and-access-rights
        [Flags]
        internal enum ServiceAccess : uint
        {
            QueryConfig = 0x0001,
            ChangeConfig = 0x0002,
            QueryStatus = 0x0004,
            EnumerateDependents = 0x0008,
            Start = 0x0010,
            Stop = 0x0020,
            PauseContinue = 0x0040,
            Interrogate = 0x0080,
            UserDefinedControl = 0x0100,

            All =
                0x000F0000 |
                QueryConfig |
                ChangeConfig |
                QueryStatus |
                EnumerateDependents |
                Start |
                Stop |
                PauseContinue |
                Interrogate |
                UserDefinedControl,
        }

        // SERVICE_CONFIG_
        // https://docs.microsoft.com/windows/win32/api/winsvc/nf-winsvc-changeserviceconfig2w
        internal enum ServiceConfigInfoLevels : uint
        {
            DESCRIPTION = 1,
            FAILURE_ACTIONS = 2,
            DELAYED_AUTO_START_INFO = 3,
            FAILURE_ACTIONS_FLAG = 4,
            SERVICE_SID_INFO = 5,
            REQUIRED_PRIVILEGES_INFO = 6,
            PRESHUTDOWN_INFO = 7,
            TRIGGER_INFO = 8,
            PREFERRED_NODE = 9,
        }

        // SERVICE_ERROR_
        internal enum ServiceErrorControl : uint
        {
            Ignore = 0x00000000,
            Normal = 0x00000001,
            Severe = 0x00000002,
            Critical = 0x00000003,
        }

        // SC_MANAGER_
        // https://docs.microsoft.com/windows/win32/services/service-security-and-access-rights
        [Flags]
        internal enum ServiceManagerAccess : uint
        {
            Connect = 0x0001,
            CreateService = 0x0002,
            EnumerateService = 0x0004,
            Lock = 0x0008,
            QueryLockStatus = 0x0010,
            ModifyBootConfig = 0x0020,

            All =
                0x000F0000 |
                Connect |
                CreateService |
                EnumerateService |
                Lock |
                QueryLockStatus |
                ModifyBootConfig,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SERVICE_DESCRIPTION
        {
            public string Description;
        }
    }
}
