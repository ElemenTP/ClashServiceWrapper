using System.Security.Principal;

namespace ClashServiceWrapper
{
    public static class Util
    {
        public static bool IsLocalSystem()
        {
            using var identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null));
        }

        public static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

    }
}
