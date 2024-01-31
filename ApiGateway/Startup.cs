using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
//using Microsoft.AspNetCore.Http;

namespace APIGateway
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
        /// Configuration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));
            services.AddOcelot();
            services.AddSwaggerForOcelot(Configuration);
            services.AddControllers();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwaggerForOcelotUI(c =>
                {
                    c.PathToSwaggerGenerator = "/swagger/docs";
                    //Add below two lines & remove "/gateway" from UpstreamPathTemplate in ocelot.json file, if pathtype is prefix and path is "/gateway/*" in AKS
                    //c.DownstreamSwaggerEndPointBasePath = "/gateway/swagger/docs";
                    //c.ReConfigureUpstreamSwaggerJson = AlterUpstreamSwaggerJson;
                    c.OAuthClientId(Configuration["AzureAd:clientId"]);
                    c.OAuthClientSecret(Configuration["AzureAd:ClientSecret"]);
                    c.OAuthScopes(new string[] { Configuration["ApiScope"] });
                });
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseOcelot().Wait();
            app.UseAuthentication();
        }

        //public string AlterUpstreamSwaggerJson(HttpContext context, string swaggerJson)
        //{            
        //    return swaggerJson.Replace("\"/", "\"gateway/");
        //}
    }
}
