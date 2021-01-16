using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using Telerik.Reporting.Cache.File;
using Telerik.Reporting.Services;
using Telerik.WebReportDesigner.Services;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace TelerikReportBuildPackageLoader.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllersWithViews();
            services.AddControllers();
            services.AddRazorPages().AddNewtonsoftJson();

            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });


            // Configure dependencies for ReportsController.
            services.TryAddSingleton<IReportServiceConfiguration>(sp =>
                new ReportServiceConfiguration
                {
                    // The default ReportingEngineConfiguration will be initialized from appsettings.json or appsettings.{EnvironmentName}.json:
                    ReportingEngineConfiguration = ConfigurationHelper.ResolveConfiguration(sp.GetService<IWebHostEnvironment>()),

                    // In case the ReportingEngineConfiguration needs to be loaded from a specific configuration file, use the approach below:
                    // ReportingEngineConfiguration = ResolveSpecificReportingConfiguration(sp.GetService<IHostingEnvironment>()),
                    HostAppId = "ReportTest",
                    Storage = new FileStorage(),
                    ReportSourceResolver = new UriReportSourceResolver()
                        .AddFallbackResolver(new UriReportSourceResolver(
                            Path.Combine(sp.GetService<IWebHostEnvironment>().ContentRootPath, "Reports")))
                });

            // Configure dependencies for ReportDesignerController.
            services.TryAddSingleton<IReportDesignerServiceConfiguration>(sp => new ReportDesignerServiceConfiguration
            {
                DefinitionStorage = new FileDefinitionStorage(
                    Path.Combine(sp.GetService<IWebHostEnvironment>().ContentRootPath, "..", "..", "..", "Report Designer", "Examples")),
                SettingsStorage = new FileSettingsStorage(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Telerik Reporting")),
                ResourceStorage = new ResourceStorage(
                    Path.Combine(sp.GetService<IWebHostEnvironment>().ContentRootPath, "..", "..", "..", "Report Designer", "Examples", "Resources")),
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }

        static IConfiguration ResolveSpecificReportingConfiguration(IWebHostEnvironment environment)
        {
            // If a specific configuration needs to be passed to the reporting engine, add it through a new IConfiguration instance.
            var reportingConfigFileName = System.IO.Path.Combine(environment.ContentRootPath, "reportingAppSettings.json");
            return new ConfigurationBuilder()
                .AddJsonFile(reportingConfigFileName, true)
                .Build();
        }
    }

    public static class ConfigurationHelper
    {
        public static IConfiguration ResolveConfiguration(IWebHostEnvironment environment)
        {
            var reportingConfigFileName = System.IO.Path.Combine(environment.ContentRootPath, "appsettings.json");
            return new ConfigurationBuilder()
                .AddJsonFile(reportingConfigFileName, true)
                .Build();
        }
    }
}
