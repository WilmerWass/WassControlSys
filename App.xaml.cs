using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using WassControlSys.Core;
using WassControlSys.ViewModels;

namespace WassControlSys
{
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Registrar servicios
            services.AddSingleton<IMonitoringService, MonitoringService>();
            services.AddSingleton<IPerformanceProfileService, PerformanceProfileService>();
            services.AddSingleton<IProcessManagerService, ProcessManagerService>();
            services.AddSingleton<ITemperatureMonitorService, TemperatureMonitorService>();
            services.AddSingleton<IDiskHealthService, DiskHealthService>();
            services.AddSingleton<ISecurityService, SecurityService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ILogService, FileLogService>();
            services.AddSingleton<ISystemInfoService, SystemInfoService>();
            services.AddSingleton<ISystemMaintenanceService, SystemMaintenanceService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IStartupService, StartupService>();
            services.AddSingleton<IServiceOptimizerService, ServiceOptimizerService>();
            services.AddSingleton<IBloatwareService, BloatwareService>();
            services.AddSingleton<IPrivacyService, PrivacyService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();

            // Registrar ViewModel
            services.AddSingleton<MainViewModel>();

            // Registrar MainWindow
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var settings = _serviceProvider.GetService<ISettingsService>();
            var loc = _serviceProvider.GetService<ILocalizationService>();
            try
            {
                var s = settings?.LoadAsync().Result;
                if (s != null)
                {
                    loc?.SetLanguageAsync(s.Language).Wait();
                    ChangeAccentColor(s.AccentColor);
                }
            }
            catch { }
            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetService<MainViewModel>();
            mainWindow.Show();
        }

        public void ChangeAccentColor(string hexColor)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                var solidBrush = new SolidColorBrush(color);
                
                // Cálculo simple para el efecto hover
                byte r = (byte)Math.Min(255, color.R + 30);
                byte g = (byte)Math.Min(255, color.G + 30);
                byte b = (byte)Math.Min(255, color.B + 30);
                var hoverColor = Color.FromRgb(r, g, b);
                var hoverBrush = new SolidColorBrush(hoverColor);

                Resources["PrimaryColor"] = color;
                Resources["PrimaryBrush"] = solidBrush;
                Resources["PrimaryHoverColor"] = hoverColor;
                Resources["PrimaryHoverBrush"] = hoverBrush;
            }
            catch { }
        }
    }
}
