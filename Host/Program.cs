using System.ComponentModel;
using System.Security.Principal;
using System.ServiceProcess;
using WinSW;
using WinSW.Native;
using static WinSW.Native.ServiceApis;

namespace ClashSvcHost
{
    public static class Program
    {
        internal static bool isFirstInstance = true;
        internal static Mutex mutex = new(true, "Global\\ClashServiceHost", out isFirstInstance);

        internal static void Main(string[] args)
        {
            if (!isFirstInstance)
            {
                Console.WriteLine("The application already has one instance running.");
                Environment.Exit(0);
            }
            if (args.Length == 0)
            {
                using var identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);
                if (principal.IsInRole(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null)))
                {
                    using WrapperService svc = new();
                    ServiceBase.Run(svc);
                }
                else
                {
                    Usage();
                }
            }
            else if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "install":
                        Install();
                        break;
                    case "uninstall":
                        Uninstall();
                        break;
                    default:
                        Usage();
                        break;
                }
            }
            else
            {
                Usage();
            }
        }

        internal static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine($"\t{Constant.exeName} [command]");
            Console.WriteLine("\tCommands:");
            Console.WriteLine(String.Format("{0,-30}", "\t\tinstall") + "Install a service to run clash on the background.");
            Console.WriteLine(String.Format("{0,-30}", "\t\tuninstall") + "Uninstall the service.");
            Environment.Exit(0);
        }

        internal static void Install()
        {
            using var scm = ServiceManager.Open(ServiceManagerAccess.CreateService);
            if (scm.ServiceExists(Constant.serviceName))
            {
                Console.WriteLine($"ERROR: A service with ID '{Constant.serviceName}' already exists.");
            }
            try
            {
                using Service svc = scm.CreateService(
                    Constant.serviceName,
                    Constant.serviceName,
                    ServiceStartMode.Manual,
                    "\"" + Constant.exeDir + Constant.exeName + "\""
                    );

                svc.SetDescription(Constant.serviceDes);
                svc.SetSecurityDescriptor();
                Console.WriteLine($"INFO: Service '{Constant.serviceName}' installed successfully.");
            }
            catch (CommandException e) when (e.InnerException is Win32Exception inner)
            {
                switch (inner.NativeErrorCode)
                {
                    case Errors.ERROR_SERVICE_EXISTS:
                        Console.WriteLine($"ERROR: A service with ID '{Constant.serviceName}' already exists.");
                        break;
                    default:
                        Throw.Command.Exception("ERROR: Failed to install the service.", inner);
                        break;
                }
            }
        }

        internal static void Uninstall()
        {
            using var scm = ServiceManager.Open(ServiceManagerAccess.Connect);
            if (!scm.ServiceExists(Constant.serviceName))
            {
                Console.WriteLine($"ERROR: Service '{Constant.serviceName}' does not exist.");
            }
            try
            {
                using var svc = scm.OpenService(Constant.serviceName);

                svc.Delete();

                Console.WriteLine($"INFO: Service '{Constant.serviceName}' was uninstalled successfully.");
            }
            catch (CommandException e) when (e.InnerException is Win32Exception inner)
            {
                switch (inner.NativeErrorCode)
                {
                    case Errors.ERROR_SERVICE_DOES_NOT_EXIST:
                        Console.WriteLine($"ERROR: Service '{Constant.serviceName}' does not exist.");
                        break;

                    case Errors.ERROR_SERVICE_MARKED_FOR_DELETE:
                        Console.WriteLine($"ERROR: {e.Message}");
                        break;

                    default:
                        Throw.Command.Exception("ERROR: Failed to uninstall the service.", inner);
                        break;
                }
            }
        }
    }
}