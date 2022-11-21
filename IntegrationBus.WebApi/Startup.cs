using System.Reflection;
using System.Text;
using IntegrationBus.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;
using IntegrationBus.WebApi.Extensions;
using IntegrationBus.WebApi.Middlewares.ResourceFilter;
using IntegrationBus.Logging;
using IntegrationBus.WebApi.Options;
using IntegrationBus.WebApi.Constants;
using IntegrationBus.Application;

namespace IntegrationBus.WebApi
{
    public sealed class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment, Assembly assembly)
        {
            this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }
        public Assembly Assembly { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCustomOptions(this.Configuration)
                .AddOptionsAndSecretsValidation();

            services.AddIntegrationBusServices("x-correlation-id")
                .AddCustomCors()
                .AddCustomRouting()
                .AddResponseCaching()
                .AddCustomHealthChecks()
                .AddCustomSwagger(this.Assembly);

            ApplicationOptions appSettings = this.Configuration.Get<ApplicationOptions>();
            services.AddLogging(httpContextOptions: options =>
            {
                options.LogRequestBody = true;
                options.LogResponseBody = true;
                options.MaxBodyLength = 32000;
                options.SkipPaths = new List<PathString>()
                {
                    "/authentication/token"
                };
            }, idempotencyOptions: options =>
            {
                options.IdempotencyHeader = appSettings.IdempotencyControl.ClientRequestIdHeader;
            });

            services.AddControllers()
                .AddCustomJsonOptions(this.Environment)
                .AddCustomMvcOptions(this.Configuration)
                .AddCustomModelValidation();

            services.AddIntegrationBusApplication();

            services.AddScoped<IdempotencyFilterAttribute>();
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            #region PreConfiguration

            lifetime.ApplicationStopping.Register(() =>
            {
                app.ApplicationServices.GetRequiredService<ILogger<Startup>>().LogInformation("Shutdown has been initiated.");
            });
            lifetime.ApplicationStopped.Register(() =>
            {
                app.ApplicationServices.GetRequiredService<ILogger<Startup>>().LogInformation("Application on stopped has been called.");
            });

            ChangeToken.OnChange(this.Configuration.GetReloadToken, () =>
            {
                app.ApplicationServices.GetRequiredService<ILogger<Startup>>().LogInformation("Options or secrets has been modified.");
            });

            #endregion

            // Configure the HTTP request pipeline.
            if (this.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseIntegrationBusServices()
                .UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                });

            app.UseNetworkLogging()
                .UseCustomExceptionHandler();

            app.UseRouting()
                .UseCors(CorsPolicyName.AllowAny)
                .UseHttpContextLogging()
                .UseIdempotencyLogging();

            app.UseHttpsRedirection();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireCors(CorsPolicyName.AllowAny);

                endpoints.MapGet("/nodeid", async (context) =>
                {
                    await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(Node.Id));
                }).RequireCors(CorsPolicyName.AllowAny);
            });

            app.UseCustomSwagger(this.Assembly);
        }
    }
}
