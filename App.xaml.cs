using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using WarmBox_Central_Monitoring_Station.Services;
using WarmBox_Central_Monitoring_Station.View;
using WarmBox_Central_Monitoring_Station.View.SetIndexs;
using WarmBox_Central_Monitoring_Station.ViewModel;
using WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs;

namespace WarmBox_Central_Monitoring_Station
{
    public partial class App : Application
    {

        public static IndexViewModel MainViewModel { get; private set; }
        public readonly ServiceProvider _serviceProvider;
        public readonly IServiceDataService _serviceDataService;
        private TcpDataService _tcpDataService;
        public static IServiceProvider ServiceProvider
        {
            get
            {
                var app = Current as App;
                return app?._serviceProvider;
            }
        }
        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            _serviceDataService = _serviceProvider.GetRequiredService<IServiceDataService>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IServiceDataService, ServiceDataService>();
            services.AddSingleton<IProcessService, ProcessService>();
            services.AddSingleton<TcpDataService>(); // 注册TCP服务
            services.AddSingleton<AlarmLogService>();

            services.AddSingleton<IndexView>();
            services.AddSingleton<LoginView>();

            services.AddSingleton<IndexViewModel>();
            services.AddTransient<LoginViewModel>();

            services.AddTransient<FactoryModeSettingsView>();
            services.AddTransient<FactoryModeSettingsViewModel>();

            // 子视图及对应的 ViewModel
            services.AddTransient<DeviceWeihu>();
            services.AddTransient<DeviceWeihuViewModel>();

            services.AddTransient<NetWorkSetView>();
            services.AddTransient<NetWorkSetViewModel>();

            services.AddTransient<AddDeviceinform>();
            services.AddTransient<AddDeviceInfoViewModel>();

            services.AddTransient<SetIndex>();
            services.AddTransient<SetIndexViewModel>();

            services.AddTransient<SetBaojinListView>();
            services.AddTransient<SetBaojinListViewModel>();

            services.AddTransient<_24hourDateViewModel>();
            services.AddTransient<_24hourDateView>();

            services.AddTransient<BedIndex>();
            services.AddTransient<BedIndexViewModel>();

            services.AddSingleton<WarmBoxViewModel>();
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string lang = LanguageService.GetCurrentLanguage();
            LanguageService.SwitchLanguage(lang);


            try
            {
                _tcpDataService = _serviceProvider.GetRequiredService<TcpDataService>();
                await _tcpDataService.StartAsync();
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse)
            {
                // 捕获端口占用异常，弹出用户友好的提示窗口
                string errorMessage = $"无法启动网络服务，端口 {_tcpDataService._port} 已被占用。\n\n" +
                                      "可能的原因：\n" +
                                      "1. 程序已在运行中（请检查任务栏或任务管理器）。\n" +
                                      "2. 其他软件（如数据库、Web服务）占用了该端口。\n\n" +
                                      "请关闭占用端口的程序，或联系管理员修改程序配置的端口号。";

                MessageBox.Show(errorMessage,
                               "启动失败 - David中央监护站",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);

                // 启动失败，直接关闭应用程序
                Application.Current.Shutdown(1);
                return; 
            }
            catch (Exception ex)
            {
                string errorMessage = $"启动TCP服务时发生未知错误：\n{ex.Message}";
                MessageBox.Show(errorMessage,
                               "启动失败 - David中央监护站",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                Application.Current.Shutdown(1);
                return;
            }

            _tcpDataService.OnDataReceived += OnTcpDataReceived;


            var loginView = _serviceProvider.GetRequiredService<LoginView>();
            var loginViewModel = ActivatorUtilities.CreateInstance<LoginViewModel>(
                _serviceProvider, loginView);
            loginView.DataContext = loginViewModel;
            loginView.Show();

        }
        public static void SetMainViewModel(IndexViewModel viewModel)
        {
            MainViewModel = viewModel;
        }

        private void OnTcpDataReceived(string data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {               
                try
                {
                    // 解析JSON数据获取设备IP
                    var jsonData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(data);
                    if (jsonData.TryGetProperty("ip", out var ipElement))
                    {
                        string deviceIp = ipElement.GetString();

                        // 1. 根据设备IP找到对应的床位ViewModel并更新数据
                        var bedViewModel = FindBedViewModelByIp(deviceIp);
                        if (bedViewModel != null)
                        {
     
                            bedViewModel.UpdateFromJsonData(data);
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ 收到未知设备 {deviceIp} 的数据: {data}");
                        }                 
                    }
                    else
                    {
                        Console.WriteLine($"❌ 数据中未找到ip字段");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 处理TCP数据时出错: {ex.Message}");
                }
            });
        }

        private BedViewModel FindBedViewModelByIp(string deviceIp)
        {
            if (MainViewModel?.CurrentView is FrameworkElement element)
            {

                if (element.DataContext is WarmBoxViewModel ViewModel)
                {
                   
                    return ViewModel.DisplayBeds?.FirstOrDefault(bed =>
                        bed.DeviceIp == deviceIp);
                }              
            }
            return null;
        }
    }
}
