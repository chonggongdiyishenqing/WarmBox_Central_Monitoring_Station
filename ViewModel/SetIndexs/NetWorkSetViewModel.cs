using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Services;

namespace WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs
{
    public class NetWorkSetViewModel : INotifyPropertyChanged
    {
        private readonly IServiceDataService _serviceDataService;

        public NetWorkSetViewModel(IServiceDataService serviceDataService)
        {
            _serviceDataService = serviceDataService;
            PingIPServiceCommand = new RelayCommand(async _ => await PingipFunctionAsync());
            SaveCommand = new RelayCommand(async _ => await SaveSettingsAsync());
            _ = LoadDevicesAsync();
        }

        #region Properties

        private string? ipaddress;
        private string? port;
        private string? pingIpaddress;
        private string? texts;

        private string _pingResult = string.Empty;
        private bool _isPinging = false;
        private ObservableCollection<string> _pingHistory = new ObservableCollection<string>();

        public string? IPaddress
        {
            get => ipaddress;
            set { ipaddress = value; OnPropertyChanged(); }
        }

        public string? Port
        {
            get => port;
            set { port = value; OnPropertyChanged(); }
        }

        public string? PingIpAddress
        {
            get => pingIpaddress;
            set { pingIpaddress = value; OnPropertyChanged(); }
        }

        public string Texts
        {
            get => texts;
            set { texts = value; OnPropertyChanged(); }
        }

        public string PingResult
        {
            get => _pingResult;
            set
            {
                _pingResult = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PingHistoryText));
            }
        }

        public bool IsPinging
        {
            get => _isPinging;
            set { _isPinging = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> PingHistory
        {
            get => _pingHistory;
            set { _pingHistory = value; OnPropertyChanged(); }
        }

        public string PingHistoryText
        {
            get => string.Join(Environment.NewLine, PingHistory);
        }

        #endregion

        #region Commands

        public ICommand PingIPServiceCommand { get; }
        public ICommand SaveCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 从应用程序资源字典中获取字符串，未找到时返回键名
        /// </summary>
        private static string GetRes(string key) =>
            Application.Current.TryFindResource(key) as string ?? key;

        private async Task LoadDevicesAsync()
        {
            try
            {
                var response = await _serviceDataService.GetListendSet();
                if (response.Success && response.Data != null)
                {
                    IPaddress = response.Data.IPaddress;
                    Port = response.Data.port;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(GetRes("LoadFailed"), ex.Message),
                    GetRes("ErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public async Task PingipFunctionAsync()
        {
            string ipAddress = PingIpAddress;
            int maxAttempts = 4;

            // 验证IP地址是否为空
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ShowMessage(GetRes("IPAddressEmpty"), GetRes("InputErrorTitle"), MessageBoxImage.Warning);
                return;
            }

            // 验证IPv4格式
            string ipPattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            if (!Regex.IsMatch(ipAddress, ipPattern))
            {
                ShowMessage(GetRes("IPAddressInvalid"), GetRes("InputErrorTitle"), MessageBoxImage.Warning);
                return;
            }

            // 清空历史记录并显示初始消息
            Application.Current.Dispatcher.Invoke(() =>
            {
                PingHistory.Clear();
                PingResult = string.Format(GetRes("PingInitialMessage"), ipAddress) + Environment.NewLine;
            });

            IsPinging = true;

            try
            {
                using var ping = new Ping();
                int successCount = 0;
                long totalRoundtripTime = 0;

                // 添加开始时间戳
                AddPingHistory(string.Format("[{0:HH:mm:ss}] " + GetRes("PingingHeader"), DateTime.Now, ipAddress));

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    PingReply reply = await ping.SendPingAsync(ipAddress, 3000);
                    string resultLine = BuildPingResultLine(reply, attempt + 1);
                    AddPingHistory(resultLine);

                    if (reply.Status == IPStatus.Success)
                    {
                        successCount++;
                        totalRoundtripTime += reply.RoundtripTime;
                    }

                    if (attempt < maxAttempts - 1)
                    {
                        await Task.Delay(1000);
                    }
                }

                long averageTime = successCount > 0 ? totalRoundtripTime / successCount : 0;
                // 添加统计信息
                AddPingHistory(Environment.NewLine + BuildStatistics(ipAddress, successCount, maxAttempts, totalRoundtripTime, averageTime));
            }
            catch (Exception ex)
            {
                AddPingHistory(string.Format(GetRes("PingError"), ex.Message));
            }
            finally
            {
                IsPinging = false;
            }
        }

        private string BuildPingResultLine(PingReply reply, int attemptNumber)
        {
            if (reply.Status == IPStatus.Success)
            {
                return string.Format(GetRes("PingReplySuccessFormat"), reply.Address, reply.RoundtripTime, reply.Options?.Ttl ?? 128);
            }
            else
            {
                return GetRes("PingRequestTimeout");
            }
        }

        private string BuildStatistics(string ipAddress, int successCount, int maxAttempts, long totalRoundtripTime, long averageTime)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(GetRes("PingStatisticsHeader"), ipAddress));
            int lost = maxAttempts - successCount;
            int lossPercent = maxAttempts > 0 ? lost * 100 / maxAttempts : 0;
            sb.AppendLine(string.Format(GetRes("PingStatisticsPackets"), maxAttempts, successCount, lost, lossPercent));
            if (successCount > 0)
            {
                sb.AppendLine(GetRes("PingStatisticsRoundtrip"));
                sb.AppendLine(string.Format(GetRes("PingStatisticsMinMaxAvg"), totalRoundtripTime / successCount, totalRoundtripTime / successCount, averageTime));
            }
            return sb.ToString();
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var listendSet = new listentset
                {
                    IPaddress = this.IPaddress,
                    port = this.Port
                };

                var response = await _serviceDataService.UpdateListendSet(listendSet);

                if (response.Success)
                {
                    MessageBox.Show(
                        GetRes("SaveSuccess"),
                        GetRes("InfoTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    await LoadDevicesAsync();
                }
                else
                {
                    MessageBox.Show(
                        string.Format(GetRes("SaveFailed"), response.Message),
                        GetRes("ErrorTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(GetRes("SaveFailed"), ex.Message),
                    GetRes("ErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddPingHistory(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PingHistory.Add(message);
                PingResult = PingHistoryText;
            });
        }

        private void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            });
        }

        #endregion

        #region Model

        public class listentset
        {
            public string IPaddress { get; set; }
            public string port { get; set; }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}