using Microsoft.Extensions.Logging;
using SSBJr.ndb.integration.Services;
using SSBJr.ndb.integration.ViewModels;
using SSBJr.ndb.integration.Views;
using System.Text.Json;

namespace SSBJr.ndb.integration
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Configure HTTP Client with service discovery support for Aspire/Docker
            var apiBaseUrl = GetApiServiceUrl();

            builder.Services.AddSingleton<HttpClient>(provider =>
            {
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(apiBaseUrl);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "SSBJr.MAUI/1.0");
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                return httpClient;
            });

            // Configure JSON serialization options
            builder.Services.Configure<JsonSerializerOptions>(options =>
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.WriteIndented = true;
            });

            // Register Services
            builder.Services.AddSingleton<IApiService, ApiService>();
            builder.Services.AddSingleton<IInfrastructureService, InfrastructureService>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();

            // Register ViewModels
            builder.Services.AddSingleton<ApiManagerViewModel>();
            builder.Services.AddSingleton<InfrastructureViewModel>();
            builder.Services.AddSingleton<MonitoringViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddTransient<MessagingConfigViewModel>();
            builder.Services.AddTransient<DatabaseConfigViewModel>();
            builder.Services.AddTransient<CacheConfigViewModel>();
            builder.Services.AddTransient<ApiDetailViewModel>();

            // Register Views
            builder.Services.AddSingleton<ApiManagerPage>();
            builder.Services.AddSingleton<InfrastructurePage>();
            builder.Services.AddSingleton<MonitoringPage>();
            builder.Services.AddSingleton<SettingsPage>();
            builder.Services.AddTransient<MessagingConfigPage>();
            builder.Services.AddTransient<DatabaseConfigPage>();
            builder.Services.AddTransient<CacheConfigPage>();
            builder.Services.AddTransient<ApiDetailPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static string GetApiServiceUrl()
        {
            // 1. Aspire: Environment variable
            var aspireUrl = Environment.GetEnvironmentVariable("ApiService__BaseUrl");
            if (!string.IsNullOrEmpty(aspireUrl))
                return aspireUrl;

            // 2. Local config (appsettings, etc.)
            var configUrl = Environment.GetEnvironmentVariable("ApiService_BaseUrl");
            if (!string.IsNullOrEmpty(configUrl))
                return configUrl;

            // 3. Fallback for Docker Compose/Aspire dev
            return "https://localhost:7080/";
        }
    }
}
