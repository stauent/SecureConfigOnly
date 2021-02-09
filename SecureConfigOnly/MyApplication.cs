using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SecureConfiguration;

namespace SecureConfigOnly
{
    public class MyApplication
    {

        private readonly IApplicationSetupConfiguration _InitialConfiguration;
        private readonly IApplicationSecrets _ApplicationSecrets;

        /// <summary>
        /// We use constructor dependency injection to the interfaces we need at runtime
        /// </summary>
        /// <param name="requirements"></param>
        public MyApplication(IApplicationSetupConfiguration InitialConfiguration, IApplicationSecrets ApplicationSecrets)
        {
            _InitialConfiguration = InitialConfiguration;
            _ApplicationSecrets = ApplicationSecrets;
        }

        /// <summary>
        /// This is the application entry point. 
        /// </summary>
        /// <returns></returns>
        internal async Task Run()
        {
            $"Application Started at {DateTime.UtcNow}".TraceInformation();

            _InitialConfiguration.TraceInformation("Dumping InitialConfiguration");
            _ApplicationSecrets.TraceInformation("Dumping ApplicationSecrets");

            // Demonstrate how to get at any connection string
            string FileLoggerConnectionString = _ApplicationSecrets.ConnectionString("FileLogger");
            FileLoggerConnectionString.TraceInformation("FileLogger connection string value");

            // Demonstrate how to get ENTIRE secret, including description and metadata
            IApplicationSecretsConnectionStrings FileLoggerSecret = _ApplicationSecrets.Secret("FileLogger");
            FileLoggerSecret.TraceInformation("FileLogger ENTIRE secret");
            foreach (SecretMetaData metaData in FileLoggerSecret.MetaDataProperties)
            {
                metaData.TraceInformation("MetaData");
            }


            $"Application Ended at {DateTime.UtcNow}".TraceInformation();

            Console.WriteLine("PRESS <ENTER> TO EXIT");
            Console.ReadKey();
        }
    }
}
