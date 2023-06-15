using System.Security.AccessControl;
using System.Security.Principal;

namespace ClashServiceWrapper
{
    public static class SingleInstance
    {
        internal const string hostMutexString = "Global\\ClashServiceHost";
        internal const string clientMutexString = "Global\\ClashServiceClient";
        private static Mutex? hmutex;
        private static Mutex? cmutex;

        public static bool HostGetIsFirstInstance()
        {
            if (hmutex != null) return true;
            hmutex = new(true, hostMutexString, out bool createdNew);
            if (!createdNew)
            {
                hmutex.Close();
                hmutex = null;
            }
            return createdNew;
        }

        public static void HostSetACL()
        {
            MutexSecurity mSec = hmutex!.GetAccessControl();
            mSec.SetAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), MutexRights.Delete | MutexRights.Modify | MutexRights.Synchronize | MutexRights.TakeOwnership, AccessControlType.Allow));
            hmutex!.SetAccessControl(mSec);
        }

        public static bool ClientGetIsFirstInstance()
        {
            if (cmutex != null) return true;
            cmutex = new(true, clientMutexString, out bool createdNew);
            if (!createdNew)
            {
                cmutex.Close();
                cmutex = null;
            }
            return createdNew;
        }

        public static void HostWaitForClient()
        {
            using Mutex tmutex = MutexAcl.OpenExisting(clientMutexString, MutexRights.Delete | MutexRights.Modify | MutexRights.Synchronize | MutexRights.TakeOwnership);
            tmutex.WaitOne();
        }

        public static void ClientWaitForHost()
        {
            using Mutex tmutex = MutexAcl.OpenExisting(hostMutexString, MutexRights.Delete | MutexRights.Modify | MutexRights.Synchronize | MutexRights.TakeOwnership);
            tmutex.WaitOne();
        }
    }
}
