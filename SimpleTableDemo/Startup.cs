using System.Globalization;
using FileManager;
using Microsoft.AspNetCore.Localization;
using Newtonsoft.Json;

namespace SimpleTableDemo;

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
        services.AddScoped<DatabaseService>();

        // --- Forwarded headers (for Caddy / reverse proxy) ---
        
        DatabaseService.SeedDatabase();

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
        services.AddControllersWithViews();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors("default");
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action=Index}/{id?}");
        });

    }

}