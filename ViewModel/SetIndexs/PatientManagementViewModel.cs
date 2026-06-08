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

        // ---------- 同步相关状态 ----------
        private bool _isSyncing;                 // 弹窗是否打开
        private bool _isSyncRunning;             // 是否正在执行同步（控制动画）
        private string _syncResultMessage;       // 显示的结果消息
        private bool _isSyncCompleted;           // 同步任务是否已完成（成功/失败）

        public bool IsSyncing
        {
            get => _isSyncing;
            set { _isSyncing = value; OnPropertyChanged(); }
        }

        public bool IsSyncRunning
        {
            get => _isSyncRunning;
            set { _isSyncRunning = value; OnPropertyChanged(); }
        }

        public string SyncResultMessage
        {
            get => _syncResultMessage;
            set { _syncResultMessage = value; OnPropertyChanged(); }
        }

        public bool IsSyncCompleted
        {
            get => _isSyncCompleted;
            set { _isSyncCompleted = value; OnPropertyChanged(); }
        }

        // 命令
        public ICommand OpenSyncCommand { get; }
        public ICommand StartSyncCommand { get; }   // 原 ConfirmSyncCommand，改为开始同步
        public ICommand CancelSyncCommand { get; }

        public PatientManagementViewModel(IServiceDataService serviceDataService, IServiceProvider serviceProvider)
        {
            _serviceDataService = serviceDataService;
            _serviceProvider = serviceProvider;

            BloodTypeList = new ObservableCollection<string> { "A型", "B型", "AB型", "O型" };

            OpenSyncCommand = new RelayCommand(() =>
            {
                ResetSyncState();
                IsSyncRunning = true;               // 打开弹窗就让圆圈旋转
                SyncResultMessage = GetRes("Syncing"); // 显示初始文字
                IsSyncing = true;
            });

            StartSyncCommand = new RelayCommand(async () =>
            {
                ResetSyncState();
                IsSyncRunning = true;               // 打开弹窗就让圆圈旋转
                SyncResultMessage = GetRes("Syncing"); // 显示初始文字
                IsSyncing = true;
                // 开始同步（此时 IsSyncRunning 已为 true，按钮禁用）
                await ExecuteSyncAsync();
            }, () => !IsSyncRunning); // 同步运行中禁用按钮

            CancelSyncCommand = new RelayCommand(() =>
            {
                IsSyncing = false;
                ResetSyncState();
            });

            Task.Run(async () => await LoadDeviceListAsync());
        }

        private void ResetSyncState()
        {
            IsSyncRunning = false;
            IsSyncCompleted = false;
            SyncResultMessage = string.Empty;
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
                    System.Diagnostics.Debug.WriteLine($"SelectedDevice changed to: {value}"); // 添加这行
                    // 设备改变时加载对应患者信息
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

        /// <summary>
        /// 加载设备编号列表（从 Device 表获取 Device_Num）
        /// </summary>
        private async Task LoadDeviceListAsync()
        {
            try
            {
                IsLoading = true;
                var devices = await _serviceDataService.GetDeviceNumbersAsync();
                DeviceList = new ObservableCollection<string>(devices);
                if (DeviceList.Any())
                {
                    // 默认选中第一个设备
                    SelectedDevice = DeviceList.First();
                }
            }
            catch (Exception ex)
            {
                // 处理异常，可记录日志或显示消息
                System.Diagnostics.Debug.WriteLine($"加载设备列表失败: {ex.Message}");
                DeviceList = new ObservableCollection<string>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 根据选中的设备编号加载患者信息
        /// </summary>
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
                    // 映射到界面字段
                    PatientId = patient.PatientId?.ToString() ?? string.Empty;
                    Name = patient.Name ?? string.Empty;
                    IsMale = patient.Gender == "男";  // 假设 Gender 存储 "男" 或 "女"
                    // 解析出生日期
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

        /// <summary>
        /// 清空所有患者信息字段
        /// </summary>
        private void ClearPatientInfo()
        {
            PatientId = string.Empty;
            Name = string.Empty;
            IsMale = false;  // 默认不选中
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


        private async Task ExecuteSyncAsync()
        {
            IsSyncCompleted = false;
            SyncResultMessage = GetRes("Syncing");
            try
            {
                // 1. 获取当前选中设备对应的 BedViewModel
                var warmBoxVM = _serviceProvider.GetRequiredService<WarmBoxViewModel>();

                // 改为用 SelectedDeviceIp 匹配
                if (string.IsNullOrEmpty(SelectedDeviceIp))
                {
                    SyncResultMessage = GetRes("SyncNoData");
                    return;
                }

                var bed = warmBoxVM.AllBeds?.FirstOrDefault(b => b.DeviceIp == SelectedDeviceIp);
                if (bed == null)
                {
                    SyncResultMessage = GetRes("SyncNoData");
                    return;
                }

                // 后续同步逻辑不变...
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
                    DeviceNum = SelectedDevice      // 设备编号仍然从 SelectedDevice 获取
                };

                IsSyncRunning = true;
                IsSyncCompleted = false;
                SyncResultMessage = GetRes("Syncing");

                var result = await _serviceDataService.SyncPatientInfoAsync(patientInfo);
                if (result.Success)
                {
                    SyncResultMessage = GetRes("SyncSuccess");
                    await LoadPatientInfoAsync();
                }
                else
                {
                    SyncResultMessage = GetRes("SyncFailed") + "\n" + result.Message;
                }
            }
            catch (Exception ex)
            {
                SyncResultMessage = GetRes("SyncFailed") + "\n" + ex.Message;
            }
            finally
            {
                // 根据需要决定是否停止动画，这里保持原样
                // IsSyncRunning = false;
                // IsSyncCompleted = true;
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

        private static string GetRes(string key) => Application.Current.TryFindResource(key) as string ?? key;

        // INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Patient 类保持不变
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