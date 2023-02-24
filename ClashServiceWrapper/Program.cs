using System.CommandLine;
using System.Text;

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
            var installCommand = new Command("install", description: "Install clash service and add firewall rules");
            installCommand.SetHandler(Install);
            rootCommand.AddCommand(installCommand);
            var uninstallCommand = new Command("uninstall", description: "Uninstall clash service and delete firewall rules");
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
            var cc = new ClientController();
            cc.StartService(argsbuilder.ToString(), mon);
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
            var cc = new ClientController();
            cc.StopService();
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
            var cc = new ClientController();
            var status = cc.QueryService();
            if (status != null)
            {
                Console.WriteLine($"{status}");
            }
            else
            {
                Console.WriteLine();
                return Task.FromResult(-5);
            }
            return Task.FromResult(0);
        }

        internal static Task<int> Service()
        {
            return Task.FromResult(0);
        }

        internal static Task<int> Install()
        {
            return Task.FromResult(0);
        }

        internal static Task<int> Uninstall()
        {
            return Task.FromResult(0);
        }
    }
}