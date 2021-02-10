using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureConfiguration;

namespace SecureHostBuilderHelper
{
    public static class HostBuilderHelper
    {
        public static string ApplicationSecretsSectionName { get; set; } = "ApplicationSecrets";
        public static string InitialConfigurationSectionName { get; set; } = "InitialConfiguration";


        /// <summary>
        /// Maintains reference to base configuration interface 
        /// </summary>
        public static IConfigurationRoot baseConfiguration
        {
            get { return (_baseConfiguration); }
        }

        private static IConfigurationRoot _baseConfiguration { get; set; }

        /// <summary>
        /// Maintains reference to InitialConfiguration interface 
        /// </summary>
        public static IApplicationSetupConfiguration appSetupConfig
        {
            get { return (_appSetupConfig); }
        }

        private static IApplicationSetupConfiguration _appSetupConfig { get; set; }
        private static IApplicationSecrets _secrets { get; set; }
        private static IServiceProvider _serviceProvider { get; set; }

        ///// <summary>
        /// This method will create an initialize a generic Host Builder 
        /// </summary>
        /// <typeparam name="TApp">Main application type. Used to access user secrets</typeparam>
        /// <param name="args">Application arguments</param>
        /// <returns>Configured IHostBuilder</returns>
        public static IHostBuilder CreateHostBuilder<TApp>(string[] args) where TApp : class
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, builder) =>
                {
                    Assembly CurrentAssembly = typeof(TApp).GetTypeInfo().Assembly;

                    // Set up configuration to read appsettings.json and override with secrets.json
                    builder.AddUserSecrets(CurrentAssembly);

                    // Bind the configuration properties to the properties in the SettingsConfig object
                    IConfiguration initialConfig = builder.Build();

                    IConfigurationSection myInitialConfig = initialConfig.GetSection(InitialConfigurationSectionName);
                    _appSetupConfig = new InitialConfiguration();
                    myInitialConfig.Bind(_appSetupConfig);

                    if (!string.IsNullOrEmpty(_appSetupConfig.KeyVaultName) && !string.IsNullOrEmpty(_appSetupConfig.KeyVaultKey))
                    {
                        // Substitute the runtime environment name in the keyvault properties
                        _appSetupConfig.KeyVaultName = _appSetupConfig.KeyVaultName.Replace("{RTE}", _appSetupConfig.RTE);
                        _appSetupConfig.KeyVaultKey = _appSetupConfig.KeyVaultKey.Replace("{RTE}", _appSetupConfig.RTE);

                        builder.AddAzureKeyVaultClient(_appSetupConfig.KeyVaultName);
                    }

                    // Build the final configuration
                    _baseConfiguration = builder.Build();

                    _secrets = _baseConfiguration.InitializeApplicationSecrets(_appSetupConfig);
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    services
                        .AddTransient<TApp>()
                        .AddSingleton<IApplicationSetupConfiguration>(sp =>
                        {
                            return (_appSetupConfig);
                        })
                        .AddSingleton<IApplicationSecrets>(sp =>
                        {
                            return (_secrets);
                        })
                        .AddSingleton<IHostEnvironment>(sp =>
                        {
                            return (hostingContext.HostingEnvironment);
                        });

                    _serviceProvider = services.BuildServiceProvider();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    ConfigureCustomLogging(hostingContext, logging, _secrets, _appSetupConfig);
                });

            return hostBuilder;
        }


        /// <summary>
        /// Creates IOC container for console apps. Injects logging and custom configuration
        /// for application to consume in its constructor. The following example shows how
        /// to launch a class called "MyApplication" as your main application. It's constructor
        /// will have logging and configuration injected into it.
        ///
        ///     ConfigurationResults<MyApplication> _configuredApplication = CreateApp<MyApplication>(args);
        ///     await _configuredApplication.myService.Run();
        /// 
        /// </summary>
        /// <typeparam name="TApp">Type of your main application class</typeparam>
        /// <param name="args">Any command line parameters you used to launch the console app are passed here</param>
        /// <returns>An ConfigurationResults object containing all the information about how this application is hosted</returns>
        public static ConfigurationResults<TApp> CreateApp<TApp>(string[] args) where TApp : class
        {
            ConfigurationResults<TApp> config = new ConfigurationResults<TApp>();
            config.builder = CreateHostBuilder<TApp>(args);
            config.myHost = config.builder.Build();
            config.myService = config.myHost.Services.GetRequiredService<TApp>();
            TraceLoggerExtension._Logger = config.myHost.Services.GetRequiredService<ILogger<TApp>>();
            TraceLoggerExtension._environmentName = _appSetupConfig.RTE;
            return (config);
        }

        /// <summary>
        /// Different types of logging are enabled based on the MyProjectSettings:EnabledLoggers: [ "File", "Console", "Debug" ]
        /// </summary>
        /// <param name="hostingContext">Generic host builder context used to configure the application</param>
        /// <param name="logging">Interface used to configure logging providers</param>
        /// <param name="applicationSecrets">Interface used to access all properties in the "ApplicationSecrets" property of the appsettings.json file</param>
        /// <param name="applicationSetupConfiguration">Interface used to access all properties in the "InitialConfiguration" property of the appsettings.json file</param>
        public static void ConfigureCustomLogging(HostBuilderContext hostingContext, ILoggingBuilder logging, IApplicationSecrets applicationSecrets, IApplicationSetupConfiguration applicationSetupConfiguration)
        {
            TraceLoggerExtension._HostEnvironment = hostingContext.HostingEnvironment;
            TraceLoggerExtension._SerializationFormat = applicationSetupConfiguration.SerializationFormat;

            logging.ClearProviders();

            if (applicationSetupConfiguration.IsLoggingEnabled)
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));

                if (applicationSetupConfiguration.IsLoggerEnabled(EnabledLoggersEnum.Debug))
                    logging.AddDebug();

                if (applicationSetupConfiguration.IsLoggerEnabled(EnabledLoggersEnum.Console))
                    logging.AddConsole();

                if (applicationSetupConfiguration.IsLoggerEnabled(EnabledLoggersEnum.File))
                {
                    // The FileLogger will be configured for log4Net local logging during development.
                    string logConnectionString = applicationSecrets.ConnectionString("FileLogger");

                    // Must set the log name prior to adding Log4Net because it must know this value
                    // before it loads the config file. It does pattern matching and substitution on the filename.
                    string logPath = null;
                    string logName = null;
                    if (!string.IsNullOrEmpty(logConnectionString))
                    {
                        string[] logParts = logConnectionString.Split(";");
                        logPath = logParts[0]?.Replace("LogPath=", "");
                        logName = logParts[1]?.Replace("LogName=", "");
                    }

                    if (!string.IsNullOrEmpty(logPath))
                    {
                        if (!Directory.Exists(logPath))
                        {
                            Directory.CreateDirectory(logPath);
                        }

                        logName = $"{logPath}\\{logName}";
                    }

                    log4net.GlobalContext.Properties["LogName"] = logName;
                    logging.AddLog4Net("log4net.config");
                }
            }
        }


    }

    /// <summary>
    /// Details on how the app "TApp" was created and configured
    /// </summary>
    /// <typeparam name="TApp">The type of the application that was configured</typeparam>
    public class ConfigurationResults<TApp> where TApp : class
    {
        public IHostBuilder builder { get; set; }
        public IHost myHost { get; set; }

        public TApp myService { get; set; }

        public ST GetService<ST>()
        {
            return myHost.Services.GetService<ST>();
        }
    }

}



