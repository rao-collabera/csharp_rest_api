using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;

namespace CollaberaAPI
{
    /// <summary>
    /// Startup Class
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// The constructor
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));
            services.AddMemoryCache();
            services.AddControllers();
            services.AddHealthChecks();
            services.AddScoped<IDbRepository, DbRepository>();
            services.AddScoped<IGraphRepository, GraphRepository>();
            services.AddScoped<IRabbitMqRepository, RabbitMqRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSwaggerGen(c =>
            {
                c.OperationFilter<SwaggerExt>();
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API" });
                //var filePath = Path.Combine(System.AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
                //c.IncludeXmlComments(filePath);
                c.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
                {
                    Description = "OAuth2.0",
                    Name = "MyCMS",
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(Configuration["AuthorizationUrl"]),
                            TokenUrl = new Uri(Configuration["TokenUrl"]),
                            Scopes = new Dictionary<string, string> {
                                {Configuration["ApiScope"], "User.Read" }
                            }
                        },
                    }
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OAuth2" },
                            Scheme = "oauth2",
                            Name = "oauth2",
                            In = ParameterLocation.Header
                        },
                        new List <string> ()
                    }
                });
            });
        }

        /// <summary>
        /// Configures the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The env.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            //{
            app.UseSwagger();
            //// Add "/api" to SwaggerEndpoint, remove "api/" in the route for all controllers => [Route("api/[controller]")] & modify the UseSwagger as shown below, if pathtype is prefix and path is /api/* in AKS.
            //app.UseSwagger(c => {
            //    c.PreSerializeFilters.Add((swaggerDoc, httpReq) => {
            //        var paths = new OpenApiPaths();
            //        foreach (var path in swaggerDoc.Paths)
            //        {
            //            paths.Add("/api" + path.Key, path.Value);
            //        }
            //        swaggerDoc.Paths = paths;
            //    });
            //});
            app.UseStaticFiles();
            app.UseSwaggerUI(c =>
            {
                c.InjectStylesheet("../custom.css");
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyCMS");
                c.OAuthClientId(Configuration["AzureAd:clientId"]);
                c.OAuthClientSecret(Configuration["AzureAd:ClientSecret"]);
                c.OAuthScopes(new string[] { Configuration["ApiScope"] });
                c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
            });
            //}
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<ResponseHandlerMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/api/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
        }
    }
}