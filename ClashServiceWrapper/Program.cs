using System.CommandLine;
using System.ServiceProcess;
using System.Text;
using static ClashServiceWrapper.ServiceApis;

namespace ClashServiceWrapper
{
    internal class Program
    {
        internal static async Task<int> Main(string[] args)
        {
            var confdirOption = new Option<string>(name: "-d", description: "set configuration directory", getDefaultValue: () => $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\.config\\clash");
            var extcontOption = new Option<string?>(name: "-ext-ctl", description: "override external controller address");
            var extuiOption = new Option<string?>(name: "-ext-ui", description: "override external ui directory");
            var conffileOptin = new Option<string?>(name: "-f", description: "specify configuration file");
            var secretOption = new Option<string?>(name: "-secret", description: "override secret for RESTful API");
            var testconfOption = new Option<bool>(name: "-t", description: "test configuration and exit");
            var versionOption = new Option<bool>(name: "-v", description: "show current version of clash");
            var rootCommand = new RootCommand("A tool that helps clash run transparently with privileges to make it easier to use tun mode.") {
                confdirOption,
                extcontOption,
                extuiOption,
                conffileOptin,
                secretOption,
                testconfOption,
                versionOption,
            };
            rootCommand.SetHandler(NormalStart, confdirOption, extcontOption, extuiOption, conffileOptin, secretOption, testconfOption, versionOption);
            var startCommand = new Command("start", description: "Start clash service without monitoring") {
                confdirOption,
                extcontOption,
                extuiOption,
                conffileOptin,
                secretOption,
                testconfOption,
                versionOption
            };
            startCommand.SetHandler(NoMonStart, confdirOption, extcontOption, extuiOption, conffileOptin, secretOption, testconfOption, versionOption);
            rootCommand.AddCommand(startCommand);
            var stopCommand = new Command("stop", description: "Stop clash service");
            stopCommand.SetHandler(Stop);
            rootCommand.AddCommand(stopCommand);
            var statusCommand = new Command("status", description: "Query clash service status");
            statusCommand.SetHandler(Status);
            rootCommand.AddCommand(statusCommand);
            var serviceCommand = new Command("service", description: "Entry for service");
            serviceCommand.SetHandler(Service);
            rootCommand.AddCommand(serviceCommand);
            var installCommand = new Command("install", description: "Install clash service");
            installCommand.SetHandler(Install);
            rootCommand.AddCommand(installCommand);
            var uninstallCommand = new Command("uninstall", description: "Uninstall clash service");
            uninstallCommand.SetHandler(Uninstall);
            rootCommand.AddCommand(uninstallCommand);

            return await rootCommand.InvokeAsync(args);
        }

        internal static Task<int> NormalStart(string confdir, string? extcont, string? extui, string? conffile, string? secret, bool testconf, bool version)
        {
            return Task.FromResult(DoStart(confdir, extcont, extui, conffile, secret, testconf, version, true));
        }

        internal static Task<int> NoMonStart(string confdir, string? extcont, string? extui, string? conffile, string? secret, bool testconf, bool version)
        {
            return Task.FromResult(DoStart(confdir, extcont, extui, conffile, secret, testconf, version, false));
        }

        private static int DoStart(string confdir, string? extcont, string? extui, string? conffile, string? secret, bool testconf, bool version, bool mon)
        {
            if (Util.IsAdministrator())
            {
                Console.WriteLine("Do not run the client with administrator privileges.");
                return -2;
            }
            if (!SingleInstance.ClientGetIsFirstInstance())
            {
                Console.WriteLine("The client already has one instance running.");
                return -1;
            }
            var argsbuilder = new StringBuilder();
            PasteArguments.AppendArgument(argsbuilder, "-d");
            PasteArguments.AppendArgument(argsbuilder, confdir);
            if (extcont != null)
            {
                PasteArguments.AppendArgument(argsbuilder, "-ext-ctl");
                PasteArguments.AppendArgument(argsbuilder, extcont);
            }
            if (extui != null)
            {
                PasteArguments.AppendArgument(argsbuilder, "-ext-ui");
                PasteArguments.AppendArgument(argsbuilder, extui);
            }
            if (conffile != null)
            {
                PasteArguments.AppendArgument(argsbuilder, "-f");
                PasteArguments.AppendArgument(argsbuilder, conffile);
            }
            if (secret != null)
            {
                PasteArguments.AppendArgument(argsbuilder, "-secret");
                PasteArguments.AppendArgument(argsbuilder, secret);
            }
            if (testconf)
            {
                PasteArguments.AppendArgument(argsbuilder, "-t");
            }
            if (version)
            {
                PasteArguments.AppendArgument(argsbuilder, "-v");
            }
            try
            {
                var cc = new ClientController();
                var status = cc.QueryService();
                if (status! == ServiceControllerStatus.Running)
                {
                    Console.WriteLine($"INFO: Service '{Constant.serviceName}' is already running.");
                    return 0;
                }
                if (mon)
                {
                    cc.StartService(argsbuilder.ToString());
                }
                else
                {
                    cc.StartServiceNoMon(argsbuilder.ToString());
                    Console.WriteLine($"INFO: Service '{Constant.serviceName}' started successfully.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: Failed to start the service. ({e.Message})");
                return -3;
            }
            return 0;
        }

        internal static Task<int> Stop()
        {
            if (Util.IsAdministrator())
            {
                Console.WriteLine("Do not run the client with administrator privileges.");
                return Task.FromResult(-2);
            }
            if (!SingleInstance.ClientGetIsFirstInstance())
            {
                Console.WriteLine("The client already has one instance running.");
                return Task.FromResult(-1);
            }
            try
            {
                var cc = new ClientController();
                var status = cc.QueryService();
                if (status! == ServiceControllerStatus.Stopped)
                {
                    Console.WriteLine($"INFO: Service '{Constant.serviceName}' is already stopped.");
                    return Task.FromResult(0);
                }
                cc.StopService();
                Console.WriteLine($"INFO: Service '{Constant.serviceName}' stopped successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: Failed to stop the service. ({e.Message})");
                return Task.FromResult(-3);
            }
            return Task.FromResult(0);
        }

        internal static Task<int> Status()
        {
            if (Util.IsAdministrator())
            {
                Console.WriteLine("Do not run the client with administrator privileges.");
                return Task.FromResult(-2);
            }
            if (!SingleInstance.ClientGetIsFirstInstance())
            {
                Console.WriteLine("The client already has one instance running.");
                return Task.FromResult(-1);
            }
            try
            {
                var cc = new ClientController();
                var status = cc.QueryService();
                if (status != null)
                {
                    Console.WriteLine($"Status: {status}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: Failed to query service status. ({e.Message})");
                return Task.FromResult(-3);
            }
            return Task.FromResult(0);
        }

        internal static Task<int> Service()
        {
            if (!Util.IsLocalSystem())
            {
                Console.WriteLine("This is the entry for the windows service, do not run directly.");
                return Task.FromResult(-2);
            }
            if (!SingleInstance.HostGetIsFirstInstance())
            {
                return Task.FromResult(-1);
            }
            SingleInstance.HostSetACL();
            using WrapperService svc = new();
            ServiceBase.Run(svc);
            return Task.FromResult(0);
        }

        internal static Task<int> Install()
        {
            if (!Util.IsAdministrator())
            {
                Console.WriteLine("This command can only be run in Administrator privilege.");
                return Task.FromResult(-2);
            }
            using var scm = ServiceManager.Open(ServiceManagerAccess.CreateService);
            if (scm.ServiceExists(Constant.serviceName))
            {
                Console.WriteLine($"ERROR: A service with ID '{Constant.serviceName}' already exists.");
                return Task.FromResult(-1);
            }
            try
            {
                using Service svc = scm.CreateService(
                    Constant.serviceName,
                    Constant.serviceName,
                    ServiceStartMode.Manual,
                    "\"" + Environment.ProcessPath! + "\" service"
                    );

                svc.SetDescription(Constant.serviceDes);
                svc.SetSecurityDescriptor();
                Console.WriteLine($"INFO: Service '{Constant.serviceName}' installed successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: Failed to install the service. ({e.Message})");
                return Task.FromResult(-3);
            }
            return Task.FromResult(0);
        }

        internal static Task<int> Uninstall()
        {
            if (!Util.IsAdministrator())
            {
                Console.WriteLine("This command can only be run in Administrator privilege.");
                return Task.FromResult(-2);
            }
            using var scm = ServiceManager.Open(ServiceManagerAccess.Connect);
            if (!scm.ServiceExists(Constant.serviceName))
            {
                Console.WriteLine($"ERROR: Service '{Constant.serviceName}' does not exist.");
                Environment.Exit(-1);
            }
            try
            {
                using var svc = scm.OpenService(Constant.serviceName);

                svc.Delete();

                Console.WriteLine($"INFO: Service '{Constant.serviceName}' was uninstalled successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: Failed to uninstall the service. ({e.Message})");
                return Task.FromResult(-3);
            }
            return Task.FromResult(0);
        }
    }
}