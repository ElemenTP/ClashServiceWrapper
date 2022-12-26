using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using static WinSW.Native.ServiceApis;

namespace WinSW.Native
{
    internal ref struct ServiceManager
    {
        private IntPtr handle;

        private ServiceManager(IntPtr handle) => this.handle = handle;

        /// <exception cref="CommandException" />
        internal static ServiceManager Open(ServiceManagerAccess access = ServiceManagerAccess.All)
        {
            var handle = OpenSCManager(null, null, access);
            if (handle == IntPtr.Zero)
            {
                Throw.Command.Win32Exception("Failed to open the service control manager database.");
            }

            return new ServiceManager(handle);
        }

        /// <exception cref="CommandException" />
        internal Service CreateService(
            string serviceName,
            string displayName,
            ServiceStartMode startMode,
            string executablePath
            )
        {
            var handle = ServiceApis.CreateService(
                this.handle,
                serviceName,
                displayName,
                ServiceAccess.All,
                ServiceType.Win32OwnProcess,
                startMode,
                ServiceErrorControl.Normal,
                executablePath,
                null,
                default,
                null,
                null,
                "");
            if (handle == IntPtr.Zero)
            {
                Throw.Command.Win32Exception("Failed to create service.");
            }

            return new Service(handle);
        }

        /// <exception cref="CommandException" />
        internal Service OpenService(string serviceName, ServiceAccess access = ServiceAccess.All)
        {
            var serviceHandle = ServiceApis.OpenService(this.handle, serviceName, access);
            if (serviceHandle == IntPtr.Zero)
            {
                Throw.Command.Win32Exception("Failed to open the service.");
            }

            return new Service(serviceHandle);
        }

        internal bool ServiceExists(string serviceName)
        {
            var serviceHandle = ServiceApis.OpenService(this.handle, serviceName, ServiceAccess.QueryStatus);
            if (serviceHandle == IntPtr.Zero)
            {
                return false;
            }

            _ = CloseServiceHandle(serviceHandle);
            return true;
        }

        public void Dispose()
        {
            if (this.handle != IntPtr.Zero)
            {
                _ = CloseServiceHandle(this.handle);
            }

            this.handle = IntPtr.Zero;
        }
    }

    internal ref struct Service
    {
        private IntPtr handle;

        internal Service(IntPtr handle) => this.handle = handle;

        /// <exception cref="CommandException" />
        internal void Delete()
        {
            if (!DeleteService(this.handle))
            {
                Throw.Command.Win32Exception("Failed to delete service.");
            }
        }

        /// <exception cref="CommandException" />
        internal void SetDescription(string description)
        {
            SERVICE_DESCRIPTION sERVICE_DESCRIPTION;
            IntPtr lpDescription = Marshal.StringToHGlobalUni(description);
            sERVICE_DESCRIPTION.lpDescription = lpDescription;
            if (!ChangeServiceConfig2(
                handle,
                ServiceConfigInfoLevels.DESCRIPTION,
                ref sERVICE_DESCRIPTION))
            {
                Throw.Command.Win32Exception("Failed to configure the description.");
            }
            Marshal.FreeHGlobal(lpDescription);
        }

        /// <exception cref="CommandException" />
        internal void SetSecurityDescriptor()
        {
            byte[] securityDescriptor = Array.Empty<byte>();
            if (!QueryServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, securityDescriptor, 0, out uint bufSizeNeeded))
            {
                if (Marshal.GetLastWin32Error() != 122)
                    Throw.Command.Win32Exception("Failed to fetch the security descriptor.");
            }
            securityDescriptor = new byte[bufSizeNeeded];
            if (!QueryServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, securityDescriptor, bufSizeNeeded, out _))
            {
                Throw.Command.Win32Exception("Failed to fetch the security descriptor.");
            }
            RawSecurityDescriptor rsd = new(securityDescriptor, 0);
            DiscretionaryAcl dacl = new(false, false, rsd.DiscretionaryAcl);
            SecurityIdentifier sid = new(WellKnownSidType.AuthenticatedUserSid, null);
            dacl.SetAccess(AccessControlType.Allow, sid, (int)(ServiceAccess.Start | ServiceAccess.Stop | ServiceAccess.QueryStatus), InheritanceFlags.None, PropagationFlags.None);
            byte[] rawdacl = new byte[dacl.BinaryLength];
            dacl.GetBinaryForm(rawdacl, 0);
            rsd.DiscretionaryAcl = new RawAcl(rawdacl, 0);
            byte[] rawsd = new byte[rsd.BinaryLength];
            rsd.GetBinaryForm(rawsd, 0);
            if (!SetServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, rawsd))
            {
                Throw.Command.Win32Exception("Failed to set the security descriptor.");
            }
        }

        public void Dispose()
        {
            if (this.handle != IntPtr.Zero)
            {
                _ = CloseServiceHandle(this.handle);
            }

            this.handle = IntPtr.Zero;
        }
    }
}
