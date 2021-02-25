using System;
using System.Threading.Tasks;
using SecureHostBuilderHelper;

namespace SecureConfigOnly
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Ensure that you are not running in a VPN or that appropriate ports are open to access Azure Resources.");

            ConfigurationResults<MyApplication> _configuredApplication = HostBuilderHelper.CreateApp<MyApplication>(args);
            await _configuredApplication.myService.Run();
        }
    }
}
