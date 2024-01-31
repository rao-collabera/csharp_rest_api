using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheck
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHealthChecks()
            .AddRabbitMQ(rabbitConnectionString: Configuration["RabbitMq:Uri"], name: "RabbitMQ", failureStatus: HealthStatus.Degraded, tags: new string[] { "RabbitMQ" })
            .AddSqlServer(connectionString: Configuration["ConnectionStrings:MyCMSConnection"], name: "SqlServer", failureStatus: HealthStatus.Degraded, tags: new string[] { "SqlServer" });
            services.AddHealthChecksUI(setupSettings: settings => { 
                settings.SetEvaluationTimeInSeconds(10);                
                settings.SetApiMaxActiveRequests(1);
                settings.SetMinimumSecondsBetweenFailureNotifications(60);
                settings.MaximumHistoryEntriesPerEndpoint(50);
            }).AddInMemoryStorage();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(config =>
            {
                config.MapHealthChecks("/api/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                config.MapHealthChecksUI(setup =>
                {
                    setup.UIPath = "/showhealth";
                    setup.ApiPath = "/healthchecks-ui";
                    setup.PageTitle = "Collabera API: HealthChecks";
                });
                config.MapDefaultControllerRoute();
            });
        }
    }
}
