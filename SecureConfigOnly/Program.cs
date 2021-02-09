using System.Threading.Tasks;
using SecureHostBuilderHelper;

namespace SecureConfigOnly
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ConfigurationResults<MyApplication> _configuredApplication = HostBuilderHelper.CreateApp<MyApplication>(args);
            await _configuredApplication.myService.Run();
        }
    }
}
