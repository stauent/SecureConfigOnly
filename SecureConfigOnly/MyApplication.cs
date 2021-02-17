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
        private readonly IConfiguration _Configuration;

        /// <summary>
        /// We use constructor dependency injection to the interfaces we need at runtime
        /// </summary>
        /// <param name="InitialConfiguration"></param>
        /// <param name="ApplicationSecrets"></param>
        /// <param name="Configuration"></param>
        public MyApplication(IApplicationSetupConfiguration InitialConfiguration, IApplicationSecrets ApplicationSecrets, IConfiguration Configuration)
        {
            _InitialConfiguration = InitialConfiguration;
            _ApplicationSecrets = ApplicationSecrets;
            _Configuration = Configuration;
        }

        /// <summary>
        /// This is the application entry point. 
        /// </summary>
        /// <returns></returns>
        internal async Task Run()
        {
            $"Application Started at {DateTime.Now.ToLongTimeString()}".TraceInformation();

            _InitialConfiguration.TraceInformation("Dumping InitialConfiguration");
            _ApplicationSecrets.TraceInformation("Dumping ApplicationSecrets");

            // Demonstrate how to get at any connection string
            string FileLoggerConnectionString = _ApplicationSecrets.ConnectionString("FileLogger");
            if (!string.IsNullOrEmpty(FileLoggerConnectionString))
            {
                FileLoggerConnectionString.TraceInformation("FileLogger connection string value");

                // Demonstrate how to get ENTIRE secret, including description and metadata
                IApplicationSecretsConnectionStrings FileLoggerSecret = _ApplicationSecrets.Secret("FileLogger");
                FileLoggerSecret.TraceInformation("FileLogger ENTIRE secret");
                foreach (SecretMetaData metaData in FileLoggerSecret.MetaDataProperties)
                {
                    metaData.TraceInformation("MetaData");
                }
            }

            // Display the JWT token that was read from Redis Cache
            string JWT = _Configuration["ONIT_JWT"];
            if (!string.IsNullOrEmpty(JWT))
            {
                JWT.TraceInformation("JWT token from cache");
            }
            else
            {
                "No JWT token was found".TraceInformation();
            }


            $"Application Ended at {DateTime.Now.ToLongTimeString()}".TraceInformation();

            Console.WriteLine("PRESS <ENTER> TO EXIT");
            Console.ReadKey();
        }
    }
}
