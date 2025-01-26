using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Localization;
using WeddingShare.BackgroundWorkers;
using WeddingShare.Configurations;
using WeddingShare.Helpers;

namespace WeddingShare
{
    public class Startup
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public static bool Ready = false;
        
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<Startup>();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var config = new ConfigHelper(new EnvironmentWrapper(), Configuration, _loggerFactory.CreateLogger<ConfigHelper>());
            
            services.AddDependencyInjectionConfiguration();
            services.AddDatabaseConfiguration(config);
            services.AddNotificationConfiguration(config);
            services.AddLocalizationConfiguration(config);

            services.AddHostedService<DirectoryScanner>();
            services.AddHostedService<NotificationReport>();
            services.AddHostedService<CleanupService>();

            services.AddRazorPages();
            services.AddControllersWithViews().AddRazorRuntimeCompilation();

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = int.MaxValue;
            });

            services.Configure<FormOptions>(x =>
            {
                x.MultipartHeadersLengthLimit = Int32.MaxValue;
                x.MultipartBoundaryLengthLimit = Int32.MaxValue;
                x.MultipartBodyLengthLimit = Int64.MaxValue;
                x.ValueLengthLimit = Int32.MaxValue;
                x.BufferBodyLengthLimit = Int64.MaxValue;
                x.MemoryBufferThreshold = Int32.MaxValue;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
            {
                options.Cookie.HttpOnly = false;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);

                options.LoginPath = "/Admin/Login";
                options.AccessDeniedPath = $"/Error?Reason={ErrorCode.Unauthorized}";
                options.SlidingExpiration = true;
            });
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.Cookie.Name = ".WeddingShare.Session";
                options.Cookie.IsEssential = true;
            });

            var localizer = services.BuildServiceProvider().GetRequiredService<IStringLocalizer<Lang.Translations>>();
            var ffmpegPath = config.GetOrDefault("FFMPEG:InstallPath", "/ffmpeg");
            var imageHelper = new ImageHelper(new FileHelper(_loggerFactory.CreateLogger<FileHelper>()), _loggerFactory.CreateLogger<ImageHelper>(), localizer);
            var downloaded = imageHelper.DownloadFFMPEG(ffmpegPath).Result;
            if (!downloaded)
            {
                _logger.LogWarning($"{localizer["FFMPEG_Download_Failed"].Value} '{ffmpegPath}'");
            }

            Ready = true;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            var config = new ConfigHelper(new EnvironmentWrapper(), Configuration, _loggerFactory.CreateLogger<ConfigHelper>());
            if (config.GetOrDefault("Settings:Force_Https", false))
            { 
                app.UseHttpsRedirection();
            }

            if (config.GetOrDefault("Security:Set_Headers", true))
            {
                try
                {
                    var logoImages = string.Join(' ', Configuration.AsEnumerable().Where(x => (x.Key.StartsWith("Settings:Logo", StringComparison.OrdinalIgnoreCase) || x.Key.StartsWith("LOGO", StringComparison.OrdinalIgnoreCase)) && (!string.IsNullOrEmpty(x.Value) && !x.Value.StartsWith(".") && !x.Value.StartsWith("/") && !x.Value.StartsWith("\\"))).Select(x => x.Value));
                    app.Use(async (context, next) =>
                    {
                        context.Response.Headers.Remove("X-Frame-Options");
                        context.Response.Headers.Append("X-Frame-Options", config.GetOrDefault("Security:X_Frame_Options", "SAMEORIGIN"));

                        context.Response.Headers.Remove("X-Content-Type-Options");
                        context.Response.Headers.Append("X-Content-Type-Options", config.GetOrDefault("Security:X_Content_Type_Options", "nosniff"));

                        context.Response.Headers.Remove("Content-Security-Policy");
                        context.Response.Headers.Append("Content-Security-Policy", config.GetOrDefault("Security:CSP_Header", $"default-src 'self' http://localhost:* ws://localhost:*; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src {(!string.IsNullOrWhiteSpace(logoImages) ? $"{logoImages} " : string.Empty)}'self' data:; frame-src 'self'; frame-ancestors 'self';"));

                        await next();
                    });
                }
                catch { }
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRequestLocalization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });
                endpoints.MapRazorPages();
            });
        }
    }
}