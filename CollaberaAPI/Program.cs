//using Azure.Extensions.AspNetCore.Configuration.Secrets;
//using Azure.Identity;
//using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
//using System;
using System.IO;

namespace CollaberaAPI
{
    /// <summary>
    /// Program Class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates the host builder.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureAppConfiguration((builder) =>
                    {
                        builder.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile("SPRepo.json");
                        //Get the connection string from Azure KeyVault
                        //var Config = builder.Build();
                        //var secretClient = new SecretClient(new Uri(Config["AzureAd:KeyVaultUrl"]), new ClientSecretCredential(Config["AzureAd:tenantId"], Config["AzureAd:ClientId"], Config["AzureAd:ClientSecret"]));
                        //builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                    });
                });
    }
}