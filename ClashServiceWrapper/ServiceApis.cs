﻿using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;

namespace ClashServiceWrapper
{
    //P/Invoke Code from WinSW: https://github.com/winsw/WinSW
    internal static partial class ServiceApis
    {
        [LibraryImport("advapi32.dll", EntryPoint = "ChangeServiceConfig2W", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ChangeServiceConfig2(IntPtr serviceHandle, ServiceConfigInfoLevels infoLevel, ref SERVICE_DESCRIPTION info);

        [LibraryImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseServiceHandle(IntPtr objectHandle);

        [LibraryImport("advapi32.dll", EntryPoint = "CreateServiceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        internal static partial IntPtr CreateService(
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
            string? dependencies,
            string? serviceStartName,
            string? password);

        [LibraryImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DeleteService(IntPtr serviceHandle);

        [LibraryImport("advapi32.dll", EntryPoint = "OpenSCManagerW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr OpenSCManager(string? machineName, string? databaseName, ServiceManagerAccess desiredAccess);

        [LibraryImport("advapi32.dll", EntryPoint = "OpenServiceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr OpenService(IntPtr databaseHandle, string serviceName, ServiceAccess desiredAccess);

        [LibraryImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool QueryServiceObjectSecurity(IntPtr serviceHandle, SecurityInfos secInfo, byte[] lpSecDesrBuf, uint bufSize, out uint bufSizeNeeded);

        [LibraryImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetServiceObjectSecurity(IntPtr serviceHandle, SecurityInfos securityInformation, byte[] securityDescriptor);

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

        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_DESCRIPTION
        {
            public IntPtr lpDescription;
        }
    }
}
