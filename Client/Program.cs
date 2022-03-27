namespace ClashSvcClient
{
    public static class Program
    {
        internal static bool isFirstInstance = true;
        internal static Mutex mutex = new(true, "Global\\ClashServiceClient", out isFirstInstance);

        internal static void Main(string[] args)
        {
            if (!isFirstInstance)
            {
                Console.WriteLine("The application already has one instance running.");
                Environment.Exit(-1);
            }
            {
                try
                {
                    using Mutex mutexHost = new(true, "Global\\ClashServiceHost", out bool isServiceStopped);
                    if (!isServiceStopped)
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    Console.WriteLine("ERROR: The service is running!");
                    Environment.Exit(-2);
                }
            }
            try
            {
                ControllerService cs = new();
                cs.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message);
                Environment.Exit(-3);
            }
        }
    }
}