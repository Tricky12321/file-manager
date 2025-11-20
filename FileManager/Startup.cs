using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using FileManager;
using FileManager.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace FileManager
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Scoped Services - <Interface, Implementation>();
            services.AddScoped<QBittorrentService>();
            services.AddScoped<FileSystemService>();

            services.AddControllersWithViews();
            
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var requestCulture = new RequestCulture("da-DK");
                requestCulture.UICulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
                requestCulture.Culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
                requestCulture.Culture.DateTimeFormat.DateSeparator = "/";
                options.DefaultRequestCulture = requestCulture;
                options.SupportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("da-DK"),
                };
                options.RequestCultureProviders.Clear();
            });

            //Add automapper used to convert db models to dto
            services.AddAutoMapper(typeof(Startup));

            //Access header
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    //Set datetime to local og hosting machine
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                    //Optimize formatting
                    options.SerializerSettings.Formatting = Formatting.None;
                    //Ignore json self ReferenceLoopHandling
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    //Don't return json null values
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services.AddCors(o => o.AddPolicy("default", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));

            services.AddControllersWithViews();
            
            // In production, the Angular files will be served from this directory
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/dist"; });
            }
            else
            {
                services.AddSpaStaticFiles(configuration => { configuration.RootPath = "/app/wwwroot"; });
            }

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment() || Environment.GetEnvironmentVariable("USE_DEV_SITE") == "true")
            {
                app.UseDeveloperExceptionPage();
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSpaStaticFiles();
            app.UseMiddleware<RequestMiddleware>();
            app.UseCors("default");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseStaticFiles();
            app.UseDefaultFiles();
            app.UseSpaStaticFiles();
            
            app.UseEndpoints(endpoints =>
            {
                // Catch-all fallback for SPA routes
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapFallbackToFile("index.html");
            });
            
            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";
                if (env.IsDevelopment() || Environment.GetEnvironmentVariable("USE_DEV_SITE") == "true")
                {
                    string angularProjectPath = spa.Options.SourcePath;
                    int port = GetFreePort();
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "ng",
                        Arguments = $"serve --port {port}",
                        WorkingDirectory = angularProjectPath,
                        UseShellExecute = true,
                    };

                    Process process = Process.Start(psi);
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:"+port);
                }
                else
                {
                    spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
                    {
                        RequestPath = "/app/wwwroot",
                        OnPrepareResponse = context => { context.Context.Response.Headers.Add("Cache-Control", "public, max-age=43200"); }
                    };
                }
            });
            Console.WriteLine($"ContentRootPath: {env.ContentRootPath}");
            Console.WriteLine($"WebRootPath:     {env.WebRootPath}");
        }
        
        private static int GetFreePort(int minPort = 45001, int maxPort = 65535)
        {
            int port = new Random().Next(minPort, maxPort);
            do
            {
                // Check if the port is available
                using (var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port))
                {
                    try
                    {
                        listener.Start();
                        listener.Stop();

                        break; // Port is available, exit the loop
                    }
                    catch
                    {
                        // If port is in use, try another one
                        port = new Random().Next(45001, 65535);
                    }
                }
            } while (port == 0);

            return port;
        }
    }
}