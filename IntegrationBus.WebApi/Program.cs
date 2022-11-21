using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using IntegrationBus.WebApi.Constants;
using IntegrationBus.WebApi.Options;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Extensions.Hosting;

namespace IntegrationBus.WebApi
{
    public static class Program
    {
        static Program()
        {
            Assembly = typeof(Program).Assembly;
            AssemblyName entryAssemblyName = Assembly.GetName();
            ApplicationName = entryAssemblyName.Name ?? string.Empty;
            ApplicationVersion = entryAssemblyName.Version ?? Version.Parse("0.0.0.0");
            ApplicationRootPath = Path.GetDirectoryName(Assembly.Location) ?? throw new ArgumentNullException(nameof(ApplicationRootPath));
            ApplicationDataDirectoryPath = Path.Combine(ApplicationRootPath, "App_Data");
        }

        internal static string ApplicationName { get; }
        internal static Version ApplicationVersion { get; }
        internal static string ApplicationRootPath { get; }
        internal static string ApplicationDataDirectoryPath { get; }
        internal static OSPlatform OSPlatform { get; private set; }
        internal static Assembly Assembly { get; }

        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            Log.Logger = CreateBootstrapLogger();

            IHostEnvironment? hostEnvironment = null;

            try
            {
                Log.Information("Building Host...");

                if (!Directory.Exists(ApplicationDataDirectoryPath))
                {
                    Directory.CreateDirectory(ApplicationDataDirectoryPath);
                }

                OSPlatform = GetOSPlatform();

                IHost host = CreateHostBuilder(args).Build();

                Log.Information("Host build successfully.");

                hostEnvironment = host.Services.GetRequiredService<IHostEnvironment>();
                Log.Information("Hosting environment is {EnvironmentName}", hostEnvironment.EnvironmentName);

                string coreCLR = ((AssemblyInformationalVersionAttribute[])typeof(object).Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>())[0].InformationalVersion;
                string coreFX = ((AssemblyInformationalVersionAttribute[])typeof(Uri).Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>())[0].InformationalVersion;

                StringBuilder logMessage = new StringBuilder()
                    .AppendLine("Application.Name: {AppName}")
                    .AppendLine("Application.Version: {AppVersion}")
                    .AppendLine("Environment.Version: {EnvVersion}")
                    .AppendLine("RuntimeInformation.FrameworkDescription: {RuntimeInfo}")
                    .AppendLine("CoreCLR Build: {CoreClrBuild}")
                    .AppendLine("CoreCLR Hash: {CoreClrHash}")
                    .AppendLine("CoreFX Build: {CoreFxBuild}")
                    .AppendLine("CoreFX Hash: {CoreFxHash}")
                    .AppendLine("Environment.OSVersion: {OSVersion}")
                    .AppendLine("RuntimeInformation.OSDescription: {OSDescription}")
                    .AppendLine("RuntimeInformation.OSArchitecture: {OSArchitecture}")
                    .AppendLine("Environment.ProcessorCount: {CpuCount}");
                Log.Information(logMessage.ToString(),
                    ApplicationName,
                    ApplicationVersion,
                    Environment.Version,
                    RuntimeInformation.FrameworkDescription,
                    coreCLR.Split('+')[0],
                    coreCLR.Split('+')[1],
                    coreFX.Split('+')[0],
                    coreFX.Split('+')[1],
                    Environment.OSVersion,
                    RuntimeInformation.OSDescription,
                    RuntimeInformation.OSArchitecture,
                    Environment.ProcessorCount);

                await host.RunAsync();

                Log.Information("{AppName} has stopped.", ApplicationName);

                return 0;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Log.Fatal(ex, "{AppName} terminated unexpectedly in {Environment} mode.", ApplicationName, hostEnvironment?.EnvironmentName);
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder hostBuilder = new HostBuilder();
            hostBuilder
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureHostConfiguration(c =>
                {
                    c.AddEnvironmentVariables(prefix: "DOTNET_");
                    if (args.Length > 0)
                    {
                        c.AddCommandLine(args);
                    }
                })
                .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
                {
                    AddConfiguration(configurationBuilder, hostBuilderContext.HostingEnvironment, args);
                })
                .UseSerilog((hostBuilderContext, serviceProvider, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(hostBuilderContext.Configuration)
                        .Enrich.WithProperty("ApplicationName", ApplicationName)
                        .Enrich.WithProperty("ApplicationVersion", ApplicationVersion)
                        .Enrich.WithProperty("NodeId", Node.Id)
                        .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                        .Enrich.WithProperty("ProcessName", Process.GetCurrentProcess().ProcessName)
                        .Enrich.WithProperty("ProcessCommandLine", Environment.CommandLine)
                        .Enrich.WithProperty("MachineName", Environment.MachineName)
                        .Enrich.WithProperty("EnvironmentName", hostBuilderContext.HostingEnvironment.EnvironmentName)
                        .Enrich.WithProperty("EnvironmentUserName", Environment.UserName)
                        .Enrich.WithProperty("OSPlatform", OSPlatform);
                })
                .UseDefaultServiceProvider((hostBuilderContext, serviceProviderOptions) =>
                {
                    bool isDevelopment = hostBuilderContext.HostingEnvironment.IsDevelopment();
                    serviceProviderOptions.ValidateScopes = isDevelopment;
                    serviceProviderOptions.ValidateOnBuild = isDevelopment;
                })
                .ConfigureWebHost(c =>
                {
                    c.UseKestrel((webHostBuilderContext, options) =>
                    {
                        options.AddServerHeader = false;
                        options.Configure(
                            webHostBuilderContext.Configuration.GetSection(nameof(ApplicationOptions.Kestrel)));
                        //options.ConfigureEndpointDefaults(listenOptions => listenOptions.Protocols = HttpProtocols.Http2);

                        ConfigureKestrelServerLimits(webHostBuilderContext, options);
                    })
                    .UseIIS()
                    .UseShutdownTimeout(TimeSpan.FromSeconds(60))
                    .UseStartup<Startup>(builderContext =>
                    {
                        return new Startup(builderContext.Configuration, builderContext.HostingEnvironment, Assembly);
                    });
                });

            return hostBuilder;
        }

        private static IConfigurationBuilder AddConfiguration(IConfigurationBuilder configurationBuilder, IHostEnvironment hostEnvironment, string[] args)
        {
            configurationBuilder
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{hostEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            if (args.Length > 0)
            {
                configurationBuilder.AddCommandLine(args);
            }

            return configurationBuilder;
        }

        /// <summary>
        /// Configure Kestrel server limits from appsettings.json is not supported. Need manually copy from config.
        /// https://github.com/aspnet/KestrelHttpServer/issues/2216
        /// </summary>
        private static void ConfigureKestrelServerLimits(WebHostBuilderContext webHostBuilderContext, KestrelServerOptions options)
        {
            var sourceOptions = new KestrelServerOptions();
            webHostBuilderContext.Configuration.GetSection(nameof(ApplicationOptions.Kestrel)).Bind(sourceOptions);

            KestrelServerLimits limits = options.Limits;
            KestrelServerLimits sourceLimits = sourceOptions.Limits;

            Http2Limits http2 = limits.Http2;
            Http2Limits sourceHttp2 = sourceLimits.Http2;

            http2.HeaderTableSize = sourceHttp2.HeaderTableSize;
            http2.InitialConnectionWindowSize = sourceHttp2.InitialConnectionWindowSize;
            http2.InitialStreamWindowSize = sourceHttp2.InitialStreamWindowSize;
            http2.MaxFrameSize = sourceHttp2.MaxFrameSize;
            http2.MaxRequestHeaderFieldSize = sourceHttp2.MaxRequestHeaderFieldSize;
            http2.MaxStreamsPerConnection = sourceHttp2.MaxStreamsPerConnection;

            limits.KeepAliveTimeout = sourceLimits.KeepAliveTimeout;
            limits.MaxConcurrentConnections = sourceLimits.MaxConcurrentConnections;
            limits.MaxConcurrentUpgradedConnections = sourceLimits.MaxConcurrentUpgradedConnections;
            limits.MaxRequestBodySize = sourceLimits.MaxRequestBodySize;
            limits.MaxRequestBufferSize = sourceLimits.MaxRequestBufferSize;
            limits.MaxRequestHeaderCount = sourceLimits.MaxRequestHeaderCount;
            limits.MaxRequestHeadersTotalSize = sourceLimits.MaxRequestHeadersTotalSize;
            // TODO https://github.com/aspnet/AspNetCore/issues/12614
            limits.MaxRequestLineSize = sourceLimits.MaxRequestLineSize - 10;
            limits.MaxResponseBufferSize = sourceLimits.MaxResponseBufferSize;
            limits.MinRequestBodyDataRate = sourceLimits.MinRequestBodyDataRate;
            limits.MinResponseDataRate = sourceLimits.MinResponseDataRate;
            limits.RequestHeadersTimeout = sourceLimits.RequestHeadersTimeout;
        }

        /// <summary>
        /// Creates a logger used during application initialization.
        /// <see href="https://nblumhardt.com/2020/10/bootstrap-logger/"/>.
        /// </summary>
        /// <returns>A logger that can load a new configuration.</returns>
        private static ReloadableLogger CreateBootstrapLogger()
        {
            return new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Debug()
                .CreateBootstrapLogger();
        }

        private static OSPlatform GetOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatform.Linux;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                return OSPlatform.FreeBSD;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatform.OSX;
            }

            return OSPlatform.Create("Unknown");
        }
    }
}