using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private TcpDataService _hl7Service;
        private string _connectionString;                 // 数据库连接字符串
        private HashSet<string> _validDeviceIps;          // 有效设备IP列表
        private static readonly object _dbLock = new();   // 历史数据写入锁

        public static IServiceProvider ServiceProvider
        {
            get
            {
                var app = Current as App;
                return app?._serviceProvider;
            }
        }
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IServiceDataService, ServiceDataService>();
            services.AddSingleton<IProcessService, ProcessService>();
            services.AddSingleton<AlarmLogService>();

            services.AddSingleton<IndexView>();
            services.AddSingleton<LoginView>();

            services.AddSingleton<IndexViewModel>();
            services.AddTransient<LoginViewModel>();

            services.AddTransient<FactoryModeSettingsView>();
            services.AddTransient<FactoryModeSettingsViewModel>();

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

            // 显示登录窗口（立即显示，不阻塞）
            var loginView = _serviceProvider.GetRequiredService<LoginView>();
            var loginViewModel = ActivatorUtilities.CreateInstance<LoginViewModel>(_serviceProvider, loginView);
            loginView.DataContext = loginViewModel;
            loginView.Show();

            // 异步初始化后台服务
            _ = InitializeServicesAsync();
        }

        private async Task InitializeServicesAsync()
        {
            // 1. 初始化数据库
            if (!InitializeDatabase())
            {
                await Dispatcher.InvokeAsync(() =>
                    MessageBox.Show("数据库初始化失败，历史数据存储和配置读取将不可用。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning)
                );
                return;
            }

            // 2. 加载配置
            if (!await LoadConfiguration())
            {
                await Dispatcher.InvokeAsync(() =>
                    MessageBox.Show("加载配置失败，HL7 监听将无法启动。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning)
                );
                return;
            }

            // 3. 启动 HL7 监听
            try
            {
                _hl7Service = new TcpDataService(_listenIp, _listenPort);
                _hl7Service.OnHl7MessageReceived += OnHl7MessageReceived;
                _hl7Service.OnClientConnected += OnClientConnected;
                _hl7Service.OnClientDisconnected += OnClientDisconnected;
                await _hl7Service.StartAsync();
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                    MessageBox.Show($"HL7 监听启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error)
                );
            }
        }

        // ==================== 数据库初始化 ====================
        private bool InitializeDatabase()
        {
            try
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                DirectoryInfo programDir = new DirectoryInfo(currentDir);
                // 根据部署调整相对路径，此处沿用原解析程序的逻辑
                DirectoryInfo rootDir = programDir.Parent?.Parent?.Parent?.Parent;
                if (rootDir == null) rootDir = programDir;

                string dbPath = Path.Combine(rootDir.FullName, "MonitoringStationData", "MonitoringStation.db");
                if (!File.Exists(dbPath))
                {
                    dbPath = Path.Combine(currentDir, "MonitoringStation.db");
                    if (!File.Exists(dbPath))
                    {
                        Console.WriteLine($"数据库文件未找到: {dbPath}");
                        return false;
                    }
                }

                _connectionString = $"Data Source={dbPath};Cache=Shared";
                Console.WriteLine($"数据库连接成功: {dbPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据库初始化失败: {ex.Message}");
                return false;
            }
        }

        // ==================== 加载配置 ====================
        private string _listenIp;
        private int _listenPort;


        private void OnClientConnected(string clientEndPoint)
        {
            string ip = clientEndPoint.Split(':')[0]; // 简化提取IP
            Application.Current.Dispatcher.Invoke(() =>
            {
                var bed = BedViewModel.GetLiveBed(ip);
                if (bed != null)
                    bed.SetOffline(false); // 标记在线
            });
        }

        private void OnClientDisconnected(string clientEndPoint)
        {
            string ip = clientEndPoint.Split(':')[0];
            Application.Current.Dispatcher.Invoke(() =>
            {
                var bed = BedViewModel.GetLiveBed(ip);
                if (bed != null)
                    bed.SetOffline(true); // 标记离线
            });
        }
        private async Task<bool> LoadConfiguration()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                await conn.OpenAsync();

                // 1. 监听配置
                using (var cmd = new SqliteCommand("SELECT listent_ip, listent_port FROM ListentSet WHERE ID = 1", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        Console.WriteLine("❌ ListentSet 表中没有配置记录");
                        return false;
                    }
                    _listenIp = reader.GetString(0);
                    _listenPort = reader.GetInt32(1);
                    Console.WriteLine($"监听配置: {_listenIp}:{_listenPort}");
                }

                // 2. 有效设备 IP 列表
                _validDeviceIps = new HashSet<string>();
                using (var cmd = new SqliteCommand("SELECT Device_ip FROM Device WHERE Device_ip IS NOT NULL AND Device_ip != ''", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        _validDeviceIps.Add(reader.GetString(0));
                }
                Console.WriteLine($"已加载 {_validDeviceIps.Count} 个有效设备IP");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置失败: {ex.Message}");
                return false;
            }
        }

        // ==================== HL7 消息处理 ====================
        private void OnHl7MessageReceived(string clientIp, string hl7Message)
        {
            // 1. 检查是否为授权设备
            if (_validDeviceIps != null && !_validDeviceIps.Contains(clientIp))
            {
                Console.WriteLine($"未授权设备 {clientIp}，忽略消息");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var bed = BedViewModel.GetLiveBed(clientIp);
                if (bed == null) return;

                bed.SetOffline(false);
                // 2. 解析 HL7 并更新 BedViewModel
                Hl7DataProcessor.ProcessMessage(clientIp, hl7Message, bed);

                // 3. 历史数据存储（每小时一条）
                _ = StoreHistoryDataAsync(clientIp, bed);
            });
        }

        // ==================== 历史数据存储 ====================
        private async Task StoreHistoryDataAsync(string deviceIp, BedViewModel bed)
        {
            DateTime now = DateTime.Now;
            DateTime hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            DateTime hourEnd = hourStart.AddHours(1);

            int? ParseInt(string text)
            {
                if (string.IsNullOrEmpty(text) || text == "--") return null;
                if (double.TryParse(text, out double d)) return (int)Math.Round(d);
                return null;
            }

            // 从 BloodPressure 字符串中解析收缩压/舒张压
            int? sys = null, dia = null;
            string bp = bed.BloodPressure;
            if (!string.IsNullOrEmpty(bp) && bp != "--/--")
            {
                string[] parts = bp.Split('/');
                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out int s)) sys = s;
                    if (int.TryParse(parts[1], out int d)) dia = d;
                }
            }

            int? hr = ParseInt(bed.HeartRate);
            int? spo2 = ParseInt(bed.BloodOxygen);
            int? resp = ParseInt(bed.RespirationRate);
            int? temp = ParseInt(bed.SkinTemp1);
            int? pi = ParseInt(bed.PI);
            int? temp2 = ParseInt(bed.SkinTemp2);

            lock (_dbLock)
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                // 检查该小时是否已有记录
                using (var checkCmd = conn.CreateCommand())
                {
                    checkCmd.CommandText = @"SELECT COUNT(*) FROM VitalSignsHistory 
                WHERE DeviceIP = @ip AND RecordTime >= @start AND RecordTime < @end";
                    checkCmd.Parameters.AddWithValue("@ip", deviceIp);
                    checkCmd.Parameters.AddWithValue("@start", hourStart.ToString("yyyy-MM-dd HH:mm:ss"));
                    checkCmd.Parameters.AddWithValue("@end", hourEnd.ToString("yyyy-MM-dd HH:mm:ss"));
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0) return;
                }

                // 插入记录
                using (var insCmd = conn.CreateCommand())
                {
                    insCmd.CommandText = @"INSERT INTO VitalSignsHistory 
                (DeviceIP, RecordTime, HR, NIBP_SYS, NIBP_DIA, SPO2, RESP, TEMP, PI, TEMP2)
                VALUES (@ip, @time, @hr, @sys, @dia, @spo2, @resp, @temp, @pi, @temp2)";
                    insCmd.Parameters.AddWithValue("@ip", deviceIp);
                    insCmd.Parameters.AddWithValue("@time", now.ToString("yyyy-MM-dd HH:mm:ss"));
                    insCmd.Parameters.AddWithValue("@hr", (object)hr ?? DBNull.Value);
                    insCmd.Parameters.AddWithValue("@sys", (object)sys ?? DBNull.Value);
                    insCmd.Parameters.AddWithValue("@dia", (object)dia ?? DBNull.Value);
                    insCmd.Parameters.AddWithValue("@spo2", (object)spo2 ?? DBNull.Value);
                    insCmd.Parameters.AddWithValue("@resp", (object)resp ?? DBNull.Value);
                    insCmd.Parameters.AddWithValue("@temp", (object)temp ?? DBNull.Value);
                    insCmd.Parameters.AddWithValue("@pi", (object)pi ?? DBNull.Value);
                    insCmd.Parameters.AddWithValue("@temp2", (object)temp2 ?? DBNull.Value);
                    insCmd.ExecuteNonQuery();
                }

                // 限制每个设备最多保留 720 条
                using (var delCmd = conn.CreateCommand())
                {
                    delCmd.CommandText = @"DELETE FROM VitalSignsHistory 
                WHERE DeviceIP = @ip AND ID NOT IN (
                    SELECT ID FROM VitalSignsHistory 
                    WHERE DeviceIP = @ip 
                    ORDER BY RecordTime DESC LIMIT 720)";
                    delCmd.Parameters.AddWithValue("@ip", deviceIp);
                    delCmd.ExecuteNonQuery();
                }
            }
        }

        // ==================== 其他 ====================
        public static void SetMainViewModel(IndexViewModel viewModel)
        {
            MainViewModel = viewModel;
        }


    }
}