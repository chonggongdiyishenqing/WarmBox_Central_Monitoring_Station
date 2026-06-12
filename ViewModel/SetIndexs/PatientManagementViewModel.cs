using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.Services;

namespace WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs
{
    public class PatientManagementViewModel : INotifyPropertyChanged
    {
        private readonly IServiceDataService _serviceDataService;
        private readonly IServiceProvider _serviceProvider;

        // 设备列表及选中项
        private ObservableCollection<string> _deviceList;
        private string _selectedDevice;

        // 患者信息字段
        private string _patientId;
        private string _name;
        private bool _isMale;
        private string _birthYear;
        private string _birthMonth;
        private string _birthDay;
        private string _gestationalAge;
        private string _weight;
        private string _height;
        private string _ageDays;
        private string _bloodType;

        // 血型列表
        private ObservableCollection<string> _bloodTypeList;

        // 是否正在加载
        private bool _isLoading;

        // ---------- 同步弹窗状态 ----------
        private bool _isSyncing;                // 正在转圈阶段
        public bool IsSyncing
        {
            get => _isSyncing;
            set
            {
                _isSyncing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPopupOpen));
                OnPropertyChanged(nameof(IsProcessingVisible));
            }
        }

        private bool _isResultShown;            // 是否显示结果（成功/失败）
        public bool IsResultShown
        {
            get => _isResultShown;
            set
            {
                _isResultShown = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPopupOpen));
                OnPropertyChanged(nameof(IsSuccessVisible));
                OnPropertyChanged(nameof(IsFailureVisible));
            }
        }

        private bool _isSuccess;                // 同步是否成功
        public bool IsSuccess
        {
            get => _isSuccess;
            set
            {
                _isSuccess = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSuccessVisible));
                OnPropertyChanged(nameof(IsFailureVisible));
            }
        }

        // 辅助可见性
        public bool IsPopupOpen => IsSyncing || IsResultShown;
        public bool IsProcessingVisible => IsSyncing;
        public bool IsSuccessVisible => IsResultShown && IsSuccess;
        public bool IsFailureVisible => IsResultShown && !IsSuccess;

        private string _syncResultMessage;
        public string SyncResultMessage
        {
            get => _syncResultMessage;
            set { _syncResultMessage = value; OnPropertyChanged(); }
        }

        // 命令
        public ICommand SyncPatientCommand { get; }
        public ICommand CloseSyncPopupCommand { get; }

        public PatientManagementViewModel(IServiceDataService serviceDataService, IServiceProvider serviceProvider)
        {
            _serviceDataService = serviceDataService;
            _serviceProvider = serviceProvider;

            BloodTypeList = new ObservableCollection<string> { "A型", "B型", "AB型", "O型" };

            SyncPatientCommand = new RelayCommand(async () => await StartSyncProcessAsync());
            CloseSyncPopupCommand = new RelayCommand(CloseSyncPopup);

            Task.Run(async () => await LoadDeviceListAsync());
        }

        private void CloseSyncPopup()
        {
            IsSyncing = false;
            IsResultShown = false;
        }

        #region 属性

        public ObservableCollection<string> DeviceList
        {
            get => _deviceList;
            set { _deviceList = value; OnPropertyChanged(); }
        }

        public string SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    OnPropertyChanged();
                    _ = LoadPatientInfoAsync();
                }
            }
        }

        public string PatientId
        {
            get => _patientId;
            set { _patientId = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public bool IsMale
        {
            get => _isMale;
            set { _isMale = value; OnPropertyChanged(); }
        }

        public string BirthYear
        {
            get => _birthYear;
            set { _birthYear = value; OnPropertyChanged(); }
        }

        public string BirthMonth
        {
            get => _birthMonth;
            set { _birthMonth = value; OnPropertyChanged(); }
        }

        public string BirthDay
        {
            get => _birthDay;
            set { _birthDay = value; OnPropertyChanged(); }
        }

        public string GestationalAge
        {
            get => _gestationalAge;
            set { _gestationalAge = value; OnPropertyChanged(); }
        }

        public string Weight
        {
            get => _weight;
            set { _weight = value; OnPropertyChanged(); }
        }

        public string Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        public string AgeDays
        {
            get => _ageDays;
            set { _ageDays = value; OnPropertyChanged(); }
        }

        public string BloodType
        {
            get => _bloodType;
            set { _bloodType = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> BloodTypeList
        {
            get => _bloodTypeList;
            set { _bloodTypeList = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _selectedDeviceIp;
        public string SelectedDeviceIp
        {
            get => _selectedDeviceIp;
            set { _selectedDeviceIp = value; OnPropertyChanged(); }
        }

        #endregion

        #region 数据加载方法

        private async Task LoadDeviceListAsync()
        {
            try
            {
                IsLoading = true;
                var devices = await _serviceDataService.GetDeviceNumbersAsync();
                DeviceList = new ObservableCollection<string>(devices);
                if (DeviceList.Any())
                {
                    SelectedDevice = DeviceList.First();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设备列表失败: {ex.Message}");
                DeviceList = new ObservableCollection<string>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadPatientInfoAsync()
        {
            if (string.IsNullOrEmpty(SelectedDevice))
            {
                ClearPatientInfo();
                return;
            }

            try
            {
                IsLoading = true;
                var patient = await _serviceDataService.GetPatientByDeviceAsync(SelectedDevice);
                if (patient != null)
                {
                    PatientId = patient.PatientId?.ToString() ?? string.Empty;
                    Name = patient.Name ?? string.Empty;
                    IsMale = patient.Gender == "男";
                    if (patient.BirthDate.HasValue)
                    {
                        BirthYear = patient.BirthDate.Value.Year.ToString();
                        BirthMonth = patient.BirthDate.Value.Month.ToString();
                        BirthDay = patient.BirthDate.Value.Day.ToString();
                    }
                    else
                    {
                        BirthYear = BirthMonth = BirthDay = string.Empty;
                    }
                    GestationalAge = patient.GestationalAge?.ToString() ?? string.Empty;
                    Weight = patient.Weight?.ToString() ?? string.Empty;
                    Height = patient.Height?.ToString() ?? string.Empty;
                    AgeDays = patient.AgeDays?.ToString() ?? string.Empty;
                    BloodType = patient.BloodType ?? string.Empty;

                    SelectedDeviceIp = patient.DeviceIp;
                }
                else
                {
                    ClearPatientInfo();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载患者信息失败: {ex.Message}");
                ClearPatientInfo();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearPatientInfo()
        {
            PatientId = string.Empty;
            Name = string.Empty;
            IsMale = false;
            BirthYear = string.Empty;
            BirthMonth = string.Empty;
            BirthDay = string.Empty;
            GestationalAge = string.Empty;
            Weight = string.Empty;
            Height = string.Empty;
            AgeDays = string.Empty;
            BloodType = string.Empty;

            SelectedDeviceIp = null;
        }

        #endregion

        #region 同步逻辑

        private async Task StartSyncProcessAsync()
        {
            var bed = GetLiveBed();
            if (bed == null)
            {
                ShowResultImmediately(false, "设备未绑定，无法同步");
                return;
            }

            // 数据完整性校验
            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(bed.PatientName) || bed.PatientName == "未绑定")
                missingFields.Add("姓名");
            if (string.IsNullOrWhiteSpace(bed.Height) || bed.Height == "--")
                missingFields.Add("身高");
            if (string.IsNullOrWhiteSpace(bed.Weight) || bed.Weight == "--")
                missingFields.Add("体重");

            if (missingFields.Count > 0)
            {
                string msg = $"同步失败：缺少信息 - {string.Join("、", missingFields)}";
                ShowResultImmediately(false, msg);
                return;
            }

            // 开始同步（带转圈动画）
            IsSyncing = true;
            IsResultShown = false;
            IsSuccess = false;
            SyncResultMessage = GetRes("Syncing");

            var syncTask = ExecuteSyncAsync(bed);
            var delayTask = Task.Delay(3000);
            await Task.WhenAll(syncTask, delayTask);

            bool success = syncTask.Result;
            IsSyncing = false;
            IsResultShown = true;
            IsSuccess = success;
            SyncResultMessage = success ? GetRes("SyncSuccess") : GetRes("SyncFailed");
        }

        private void ShowResultImmediately(bool success, string message)
        {
            IsSyncing = false;          // 不显示转圈
            IsResultShown = true;
            IsSuccess = success;
            SyncResultMessage = message;
        }

        // 辅助方法：获取当前设备对应的 BedViewModel（假设已实现）
        private BedViewModel GetLiveBed()
        {
            if (string.IsNullOrEmpty(SelectedDeviceIp)) return null;
            return BedViewModel.GetLiveBed(SelectedDeviceIp);
        }

        private async Task<bool> ExecuteSyncAsync(BedViewModel bed)
        {
            try
            {
              
                if (bed == null)
                {
                    SyncResultMessage = GetRes("SyncNoData");
                    return false;
                }

                var patientInfo = new PatientSyncInfo
                {
                    PatientId = bed.PatientId > 0 ? bed.PatientId : null,
                    Name = bed.PatientName,
                    Gender = bed.Sex,
                    BirthDate = ParseBirthDate(bed.BirthYear, bed.BirthMonth, bed.BirthDay),
                    GestationalAge = bed.PatientTaiLing,
                    Weight = decimal.TryParse(bed.Weight, out var w) ? w : (decimal?)null,
                    Height = decimal.TryParse(bed.Height, out var h) ? h : (decimal?)null,
                    AgeDays = int.TryParse(bed.PatientOld, out var d) ? d : (int?)null,
                    BloodType = bed.BloodType,
                    DeviceNum = SelectedDevice
                };

                var result = await _serviceDataService.SyncPatientInfoAsync(patientInfo);
                if (result.Success)
                {
                    string chuangwei = ExtractChuangweiFromMessage(result.Message);
                    if (!string.IsNullOrEmpty(chuangwei))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            bed.BedNumber = chuangwei;
                        });
                    }
                    await LoadPatientInfoAsync();
                    return true;
                }
                else
                {
                    SyncResultMessage = GetRes("SyncFailed") + "\n" + result.Message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                SyncResultMessage = GetRes("SyncFailed") + "\n" + ex.Message;
                return false;
            }
        }

        private DateTime? ParseBirthDate(string year, string month, string day)
        {
            if (int.TryParse(year, out int y) && int.TryParse(month, out int m) && int.TryParse(day, out int d))
            {
                if (y >= 1900 && m >= 1 && m <= 12 && d >= 1 && d <= 31)
                    return new DateTime(y, m, d);
            }
            return null;
        }
        private static string ExtractChuangweiFromMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            int idx = message.LastIndexOf('|');
            if (idx >= 0 && idx < message.Length - 1)
                return message.Substring(idx + 1);
            return null;
        }
        private static string GetRes(string key) => Application.Current.TryFindResource(key) as string ?? key;

        #endregion

        // INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Patient
    {
        public int? PatientId { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? GestationalAge { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public int? AgeDays { get; set; }
        public string BloodType { get; set; }

        public string? DeviceIp { get; set; }
    }
}