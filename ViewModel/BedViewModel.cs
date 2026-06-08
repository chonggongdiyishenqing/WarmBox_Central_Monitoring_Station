using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.Services;

namespace WarmBox_Central_Monitoring_Station.ViewModel
{
    public enum AlarmPriority { High, Medium, Low }

    public class AlarmInfo
    {
        public string Name { get; set; }
        public AlarmPriority Priority { get; set; }
        public int RemainingCount { get; set; }
    }

    public class BedViewModel : INotifyPropertyChanged
    {

        // ==================== 基础设备信息 ====================
        private string _deviceIp;
        private string _devicetype;
        private string _devicename;

        // ==================== 患者信息 ====================
        private int _patientId;
        private string _sex;
        private string _patientName;
        private string _bedNumber;
        private string _patientTaiLing;
        private string _patientOld;

        private string _birthYear;
        private string _birthMonth;
        private string _birthDay;
        private string _weightPatient;  // 体重(g)
        private string _heightPatient;  // 身高(cm)
        private string _bloodType;

        // ==================== 生理参数 ====================
        private string _heartRate;
        private string _bloodOxygen;
        private string _respirationRate;
        private string _bloodPressure;
        private string _boxtemp;
        private string _skintemp1;
        private string _skintemp2;
        private string _humidity;
        private string _O2;
        private string _weight;
        private string _pi;
        private string _pr;
        private string _pv1;
        private string _sphb;
        private string _spoc;
        private string _spmet;
        private string _spco;

        // ==================== 波形数据 ====================
        private string _ecto2WaveData;
        private string _pletchWaveData;
        private string _hr_Ⅱ;
        private string _hr_RA;
        private string _hr_RL;
        private string _hr_LA;
        private string _hr_LL;
        private string _hr_Ⅴ;

        // 波形暂停状态
        private bool _isWaveformPaused;
        public bool IsWaveformPaused
        {
            get => _isWaveformPaused;
            set
            {
                if (_isWaveformPaused != value)
                {
                    _isWaveformPaused = value;
                    OnPropertyChanged();
                    // 可选：按钮显示文本也可通过此属性动态切换
                    OnPropertyChanged(nameof(FreezeButtonText));
                }
            }
        }

        public string FreezeButtonText
        {
            get
            {
                string key = IsWaveformPaused ? "ResumeWaveform" : "FreezeWaveform";
                object resource = Application.Current.TryFindResource(key);
                return resource as string ?? (IsWaveformPaused ? "恢复波形" : "冻结波形");
            }
        }

        // 暂停/恢复命令
        public ICommand PauseWaveformCommand { get; }

        // ==================== 报警相关 ====================
        private string _waringinform;
        private string _currentAlarm;
        private Brush _currentAlarmColor = Brushes.Transparent;
        private HashSet<string> _lastAlarmNames = new HashSet<string>();
        private List<AlarmInfo> _currentAlarmList = new List<AlarmInfo>();
        private int _currentAlarmIndex = 0;
        private DispatcherTimer _alarmTimer;
       
        private double _alarmDisplayDuration = 5.0;    // 每条报警显示总时长（秒）
        private string _isqibo;

        // ==================== UI 状态 ====================
        private bool _isSelected;
        private bool _isBound;
        private bool _isOffline;
        private bool _isPatientNameVisible = true;
        private string _workMode;
        private int _workModePercent;

        // ==================== 线程调度器 ====================
        private Dispatcher _dispatcher;

        // ==================== 事件 ====================
        public event Action<string> Etco2WaveDataUpdated;
        public event Action<string> PletchWaveDataUpdated;
        public event Action<string> hr2WaveDataUpdated;
        public event Action<string> hrraWaveDataUpdated;
        public event Action<string> hrrlWaveDataUpdated;
        public event Action<string> hrlaWaveDataUpdated;
        public event Action<string> hrllWaveDataUpdated;
        public event Action<string> hr5WaveDataUpdated;

        // ==================== 报警优先级映射表（完整版需根据说明书补充所有报警） ====================
        private static readonly Dictionary<string, AlarmPriority> AlarmPriorityMap = new()
       {
           // ===== High =====
           {"系统板与主板通讯故障", AlarmPriority.High},
           {"控制仪与传感器盒通讯故障", AlarmPriority.High},
           {"主从机通讯故障", AlarmPriority.High},
           {"从机间通讯故障", AlarmPriority.High},
           {"备份配置数据异常", AlarmPriority.High},
           {"箱篷故障", AlarmPriority.High},
           {"升降系统故障", AlarmPriority.High},
           {"辐射门故障", AlarmPriority.High},
           {"培养箱加热系统故障", AlarmPriority.High},
           {"辐射加热系统故障", AlarmPriority.High},
           {"床垫加热系统故障", AlarmPriority.High},
           {"传感器盒放置错误报警", AlarmPriority.High},
           {"箱温传感器故障", AlarmPriority.High},
           {"独立箱温传感器故障", AlarmPriority.High},
           {"箱温传感器差异", AlarmPriority.High},
           {"风道温度传感器故障", AlarmPriority.High},
           {"床温传感器故障", AlarmPriority.High},
           {"床温独立传感器故障", AlarmPriority.High},
           {"床温传感器差异故障", AlarmPriority.High},
           {"肤温传感器1故障", AlarmPriority.High},
           {"独立肤温传感器故障", AlarmPriority.High},
           {"肤温传感器2故障", AlarmPriority.High},
           {"肤温传感器差异", AlarmPriority.High},
           {"体重秤传感器未连接", AlarmPriority.High},
           {"箱温超温", AlarmPriority.High},
           {"床温超温", AlarmPriority.High},
           {"肤温超温", AlarmPriority.High},
           {"风道超温", AlarmPriority.High},
           {"肤温传感器放置错误", AlarmPriority.High},
           {"风机故障", AlarmPriority.High},
           {"控制仪内风扇故障", AlarmPriority.High},
           {"O₂传感器1故障", AlarmPriority.High},
           {"O₂传感器2故障", AlarmPriority.High},
           {"O₂传感器差异", AlarmPriority.High},
           {"血氧模块通信中断", AlarmPriority.High},
           {"血氧系统故障", AlarmPriority.High},
           {"血氧诊断故障", AlarmPriority.High},
           {"血氧导电线未连接", AlarmPriority.High},
           {"血氧导电线过期", AlarmPriority.High},
           {"血氧导电线不匹配", AlarmPriority.High},
           {"血氧无法识别导电线", AlarmPriority.High},
           {"血氧导电线故障", AlarmPriority.High},
           {"血氧传感器连接错误", AlarmPriority.High},
           {"血氧传感器过期", AlarmPriority.High},
           {"血氧传感器故障", AlarmPriority.High},
           {"血氧传感器无法识别", AlarmPriority.High},
           {"检查血氧导电线或传感器", AlarmPriority.High},
           {"未连接血氧粘黏探头", AlarmPriority.High},
           {"血氧粘黏探头过期", AlarmPriority.High},
           {"血氧粘黏探头不匹配", AlarmPriority.High},
           {"无法识别血氧粘黏探头", AlarmPriority.High},
           {"血氧粘黏探头故障", AlarmPriority.High},
           {"血氧传感器脱落", AlarmPriority.High},
           {"检查血氧传感器连接", AlarmPriority.High},
           {"ECG通信中断", AlarmPriority.High},
           {"RESP通信中断", AlarmPriority.High},
           {"NIBP模块禁用", AlarmPriority.High},
           {"NIBP通信中断", AlarmPriority.High},
           {"NIBP自检失败", AlarmPriority.High},
           {"唤醒模块通信中断", AlarmPriority.High},
           {"唤醒器连接错误", AlarmPriority.High},
           {"打印机错误", AlarmPriority.High},
           {"打印机通信中断", AlarmPriority.High},
           {"湿度加热系统故障报警", AlarmPriority.High},
           {"湿度传感器报警", AlarmPriority.High},
           {"低压报警", AlarmPriority.High},
           {"电池连接故障", AlarmPriority.High},
           {"镍氢电池报警", AlarmPriority.High},
           {"锂电池1硬件故障", AlarmPriority.High},
           {"锂电池2硬件故障", AlarmPriority.High},
           {"锂电池硬件故障", AlarmPriority.High},
           {"ECG导联脱落", AlarmPriority.High},
           {"ECG V 导联脱落", AlarmPriority.High},
           {"ECG过载", AlarmPriority.High},
           {"NIBP袖带错误", AlarmPriority.High},
           {"NIBP硬件错误", AlarmPriority.High},
           {"CO₂硬件错误", AlarmPriority.High},
           {"CO₂软件错误", AlarmPriority.High},
           {"CO₂电机转速超限", AlarmPriority.High},
           {"CO₂出厂未校准", AlarmPriority.High},
           {"CO₂采样管堵塞", AlarmPriority.High},
           {"CO₂没有采样管", AlarmPriority.High},
           {"CO₂超出精度范围", AlarmPriority.High},
           {"CO₂温度超界", AlarmPriority.High},
           {"CO₂大气压力超限", AlarmPriority.High},
           {"CO₂需要校零", AlarmPriority.High},
           {"CO₂禁止校零", AlarmPriority.High},
           {"CO₂正在校零", AlarmPriority.High},
           {"CO₂校准失败", AlarmPriority.High},
           {"CO₂正在校准", AlarmPriority.High},
           {"打印机缺纸", AlarmPriority.High},
           {"水箱放置错误报警", AlarmPriority.High},
           {"缺水报警", AlarmPriority.High},
           {"血氧低信号质量", AlarmPriority.High},
           {"无效的SpO₂", AlarmPriority.High},
           {"低可信度的PR", AlarmPriority.High},
           {"无效的PR", AlarmPriority.High},
           {"低可信度的PI", AlarmPriority.High},
           {"无效的PI", AlarmPriority.High},
           {"无效平滑的PI", AlarmPriority.High},
           {"低可信度的SpCO", AlarmPriority.High},
           {"低血流灌注SpCO", AlarmPriority.High},
           {"无效的SpCO", AlarmPriority.High},
           {"低可信度的SpMet", AlarmPriority.High},
           {"低血流灌注SpMet", AlarmPriority.High},
           {"无效的SpMet", AlarmPriority.High},
           {"低可信度的SpHb", AlarmPriority.High},
           {"低血流灌注SpHb", AlarmPriority.High},
           {"无效的SpHb", AlarmPriority.High},
           {"低可信度的SpOC", AlarmPriority.High},
           {"低血流灌注SpOC", AlarmPriority.High},
           {"无效的SpOC", AlarmPriority.High},
           {"低可信度的PVI", AlarmPriority.High},
           {"无效的PVI", AlarmPriority.High},
           {"低血流灌注", AlarmPriority.High},
           {"血氧传感器初始化", AlarmPriority.High},
           {"搜寻脉搏", AlarmPriority.High},
           {"血氧探测到干扰", AlarmPriority.High},
           {"血氧粘黏探头将要过期", AlarmPriority.High},
           {"摇床板与主板通信故障", AlarmPriority.High},
           {"仅限SpO₂模式", AlarmPriority.High},
           {"血氧导电线将要过期", AlarmPriority.High},
           {"血氧传感器不匹配", AlarmPriority.High},
           {"血氧传感器将要过期", AlarmPriority.High},
           {"前门打开", AlarmPriority.High},
           {"温度上偏差", AlarmPriority.High},
           {"温度下偏差", AlarmPriority.High},
           {"床温上偏差", AlarmPriority.High},
           {"床温下偏差", AlarmPriority.High},
           {"设置报警A", AlarmPriority.High},
           {"设置报警C", AlarmPriority.High},
           {"请检查肤温", AlarmPriority.High},
           {"手控检查报警", AlarmPriority.High},
           {"O₂上偏差", AlarmPriority.High},
           {"O₂下偏差", AlarmPriority.High},
           {"停搏", AlarmPriority.High},
           {"室颤/室速", AlarmPriority.High},
           {"窒息", AlarmPriority.High},
           {"SpO₂高", AlarmPriority.High},
           {"SpO₂低", AlarmPriority.High},
           {"PR高", AlarmPriority.High},
           {"PR低", AlarmPriority.High},
           {"SpHb高", AlarmPriority.High},
           {"SpHb低", AlarmPriority.High},
           {"SpOC高", AlarmPriority.High},
           {"SpOC低", AlarmPriority.High},
           {"SpMet高", AlarmPriority.High},
           {"SpMet低", AlarmPriority.High},
           {"SpCO高", AlarmPriority.High},
           {"SpCO低", AlarmPriority.High},
           {"PI高", AlarmPriority.High},
           {"PI低", AlarmPriority.High},
           {"PVI高", AlarmPriority.High},
           {"PVI低", AlarmPriority.High},
           {"心率高", AlarmPriority.High},
           {"心率低", AlarmPriority.High},
           {"呼吸率高", AlarmPriority.High},
           {"呼吸率低", AlarmPriority.High},
           {"NIBP收缩压高", AlarmPriority.High},
           {"NIBP收缩压低", AlarmPriority.High},
           {"NIBP舒张压高", AlarmPriority.High},
           {"NIBP舒张压低", AlarmPriority.High},
           {"NIBP平均压高", AlarmPriority.High},
           {"NIBP平均压低", AlarmPriority.High},
           {"CO₂通信中断", AlarmPriority.High},
           {"EtCO₂高", AlarmPriority.High},
           {"EtCO₂低", AlarmPriority.High},
           {"FiCO₂高", AlarmPriority.High},
           {"FiCO₂低", AlarmPriority.High},
           {"BR高", AlarmPriority.High},
           {"BR低", AlarmPriority.High},
           {"湿度上偏差", AlarmPriority.High},
           {"湿度下偏差", AlarmPriority.High},
           {"血氧饱和度低于85%", AlarmPriority.High},
       
           // ===== Medium =====
           {"NIBP袖带微弱", AlarmPriority.Medium},
           {"NIBP测量超界", AlarmPriority.Medium},
           {"NIBP袖带过压", AlarmPriority.Medium},
           {"NIBP测量超时", AlarmPriority.Medium},
           {"NIBP信号饱和", AlarmPriority.Medium},
           {"NIBP测量中断", AlarmPriority.Medium},
           {"NIBP袖带类型错误", AlarmPriority.Medium},
           {"NIBP袖带漏气", AlarmPriority.Medium},
           {"NIBP气动堵塞", AlarmPriority.Medium},
           {"NIBP过分运动", AlarmPriority.Medium},
           {"锂电池1电压过低", AlarmPriority.Medium},
           {"锂电池2电压过低", AlarmPriority.Medium},
           {"锂电池电量低", AlarmPriority.Medium},
           // 以下部分如果认为仍是严重问题也可保留在 High，这里暂且分到 Medium 供调整
           // 可以按需再移动
       };

        // 注意：未出现在上述映射中的报警名称，在 BedViewModel 中会默认使用 AlarmPriority.Medium。

        private AlarmPriority? _currentAlarmPriority;
        public AlarmPriority? CurrentAlarmPriority
        {
            get => _currentAlarmPriority;
            set { _currentAlarmPriority = value; OnPropertyChanged(); }
        }

        public event Action<AlarmPriority?> AlarmPriorityChanged;

        private AlarmPriority? _previousPriority;// 记录上次优先级

        // ==================== 构造函数 ====================
        public BedViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            // 初始化报警定时器
            _alarmTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_alarmDisplayDuration)
            };
            _alarmTimer.Tick += AlarmTimer_Tick;

            // 默认值
            HeartRate = "--";
            BloodOxygen = "--";
            RespirationRate = "--";
            BloodPressure = "--/--";
            IsOffline = true;

            PauseWaveformCommand = new RelayCommand(_ =>
            {
                IsWaveformPaused = !IsWaveformPaused;

                string key = IsWaveformPaused ? "WaveformFrozen" : "WaveformResumed";
                object resource = Application.Current.TryFindResource(key);
                string message = resource as string ?? (IsWaveformPaused ? "已冻结" : "已恢复");

                string colorHex = IsWaveformPaused ? "#FF4444" : "#44FF44"; // 红色表示冻结，绿色表示恢复
                ToastHelper.Show(message, colorHex);
            });
        }

        // ==================== 公共属性 ====================
        public bool IsOffline
        {
            get => _isOffline;
            set { if (_isOffline != value) { _isOffline = value; OnPropertyChanged(); } }
        }

        public string CurrentAlarm
        {
            get => _currentAlarm;
            private set { _currentAlarm = value; OnPropertyChanged(); }
        }

        public Brush CurrentAlarmColor
        {
            get => _currentAlarmColor;
            set { _currentAlarmColor = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        public bool IsBound
        {
            get => _isBound;
            set { _isBound = value; OnPropertyChanged(); }
        }

        public bool IsPatientNameVisible
        {
            get => _isPatientNameVisible;
            set
            {
                _isPatientNameVisible = value;
                OnPropertyChanged(nameof(IsPatientNameVisible));
                OnPropertyChanged(nameof(PatientNameForDisplay));
            }
        }

        public string PatientNameForDisplay
        {
            get
            {
                if (!IsPatientNameVisible) return "***";
                if (!string.IsNullOrEmpty(PatientName) && PatientName != "--") return PatientName;
                if (!string.IsNullOrEmpty(DeviceType) && DeviceType != "--") return DeviceType;
                return string.Empty;
            }
        }

        public string WorkMode
        {
            get => _workMode;
            set { _workMode = value; OnPropertyChanged(); }
        }

        public int WorkModePercent
        {
            get => _workModePercent;
            set { _workModePercent = value; OnPropertyChanged(); }
        }

        public int PatientId
        {
            get => _patientId;
            set { _patientId = value; OnPropertyChanged(); }
        }

        public string DeviceType
        {
            get => _devicetype;
            set { _devicetype = value; OnPropertyChanged(); }
        }

        public string DeviceName
        {
            get => _devicename;
            set { _devicename = value; OnPropertyChanged(); }
        }

        public string DeviceIp
        {
            get => _deviceIp;
            set { _deviceIp = value; OnPropertyChanged(); }
        }

        public string PatientName
        {
            get => _patientName;
            set { _patientName = value; OnPropertyChanged(nameof(PatientName)); OnPropertyChanged(nameof(PatientNameForDisplay)); }
        }

        public string BedNumber
        {
            get => _bedNumber;
            set { _bedNumber = value; OnPropertyChanged(); }
        }

        public string PatientOld
        {
            get => _patientOld;
            set { _patientOld = value; OnPropertyChanged(); }
        }

        public string PatientTaiLing
        {
            get => _patientTaiLing;
            set { _patientTaiLing = value; OnPropertyChanged(); }
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


        public string Height
        {
            get => _heightPatient;
            set { _heightPatient = value; OnPropertyChanged(); }
        }

        public string BloodType
        {
            get => _bloodType;
            set { _bloodType = value; OnPropertyChanged(); }
        }

        public string Qibo
        {
            get => _isqibo;
            set { _isqibo = value; OnPropertyChanged(); }
        }

        public string HeartRate
        {
            get => _heartRate;
            set { _heartRate = value; OnPropertyChanged(); }
        }

        public string BloodOxygen
        {
            get => _bloodOxygen;
            set { _bloodOxygen = value; OnPropertyChanged(); }
        }

        public string RespirationRate
        {
            get => _respirationRate;
            set { _respirationRate = value; OnPropertyChanged(); }
        }

        public string BloodPressure
        {
            get => _bloodPressure;
            set { _bloodPressure = value; OnPropertyChanged(); }
        }

        public string Sex
        {
            get => _sex;
            set { _sex = value; OnPropertyChanged(); }
        }

        public string BoxTemp
        {
            get => _boxtemp;
            set { _boxtemp = value; OnPropertyChanged(); }
        }

        public string SkinTemp1
        {
            get => _skintemp1;
            set { _skintemp1 = value; OnPropertyChanged(); }
        }

        public string SkinTemp2
        {
            get => _skintemp2;
            set { _skintemp2 = value; OnPropertyChanged(); }
        }

        public string Humidity
        {
            get => _humidity;
            set { _humidity = value; OnPropertyChanged(); }
        }

        public string O2
        {
            get => _O2;
            set { _O2 = value; OnPropertyChanged(); }
        }

        public string Weight
        {
            get => _weight;
            set { _weight = value; OnPropertyChanged(); }
        }

        public string PI
        {
            get => _pi;
            set { _pi = value; OnPropertyChanged(); }
        }

        public string PR
        {
            get => _pr;
            set { _pr = value; OnPropertyChanged(); }
        }

        public string PV1
        {
            get => _pv1;
            set { _pv1 = value; OnPropertyChanged(); }
        }

        public string SpHb
        {
            get => _sphb;
            set { _sphb = value; OnPropertyChanged(); }
        }

        public string SpOC
        {
            get => _spoc;
            set { _spoc = value; OnPropertyChanged(); }
        }

        public string SpMet
        {
            get => _spmet;
            set { _spmet = value; OnPropertyChanged(); }
        }

        public string SpCO
        {
            get => _spco;
            set { _spco = value; OnPropertyChanged(); }
        }

        // ==================== 波形属性 ====================
        public string Ecto2WaveData
        {
            get => _ecto2WaveData;
            set { _ecto2WaveData = value; OnPropertyChanged(); Etco2WaveDataUpdated?.Invoke(value); }
        }

        public string PletchWaveData
        {
            get => _pletchWaveData;
            set { _pletchWaveData = value; OnPropertyChanged(); PletchWaveDataUpdated?.Invoke(value); }
        }

        public string Hr_Ⅱ
        {
            get => _hr_Ⅱ;
            set { _hr_Ⅱ = value; OnPropertyChanged(); hr2WaveDataUpdated?.Invoke(value); }
        }

        public string Hr_Ⅴ
        {
            get => _hr_Ⅴ;
            set { _hr_Ⅴ = value; OnPropertyChanged(); hr5WaveDataUpdated?.Invoke(value); }
        }

        public string Hr_RA
        {
            get => _hr_RA;
            set { _hr_RA = value; OnPropertyChanged(); hrraWaveDataUpdated?.Invoke(value); }
        }

        public string Hr_RL
        {
            get => _hr_RL;
            set { _hr_RL = value; OnPropertyChanged(); hrrlWaveDataUpdated?.Invoke(value); }
        }

        public string Hr_LA
        {
            get => _hr_LA;
            set { _hr_LA = value; OnPropertyChanged(); hrlaWaveDataUpdated?.Invoke(value); }
        }

        public string Hr_LL
        {
            get => _hr_LL;
            set { _hr_LL = value; OnPropertyChanged(); hrllWaveDataUpdated?.Invoke(value); }
        }

        // ==================== 报警字符串属性（核心） ====================
        public string WaringInform
        {
            get => _waringinform;
            set
            {
                if (!_dispatcher.CheckAccess())
                {
                    _dispatcher.Invoke(() => WaringInform = value);
                    return;
                }

                if (_waringinform == value) return;
                _waringinform = value;
                OnPropertyChanged();

                var newNames = string.IsNullOrWhiteSpace(value)
                    ? new HashSet<string>()
                    : value.Split('，').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToHashSet();

                if (newNames.SetEquals(_lastAlarmNames))
                    return; // 报警集合未变

                _lastAlarmNames = newNames;

                if (newNames.Count == 0)
                {
                    _currentAlarmList.Clear();
                    _alarmTimer.Stop();
                    CurrentAlarm = null;
                    CurrentAlarmColor = Brushes.Transparent;

                    
                    if (_previousPriority != null)
                    {
                        _previousPriority = null;
                        CurrentAlarmPriority = null;
                        AlarmPriorityChanged?.Invoke(null);
                    }
                    return;
                }

                // 生成排序列表
                _currentAlarmList = newNames
                .Select(name => new AlarmInfo
                {
                    Name = name,
                    Priority = AlarmPriorityMap.TryGetValue(name, out var p) ? p : AlarmPriority.Medium
                })
                .OrderBy(a => a.Priority)
                .ThenBy(a => a.Name)
                .ToList();

                _currentAlarmIndex = 0;
                ShowCurrentAlarm();
                _alarmTimer.Start();
            }
        }

        // ==================== 报警定时器回调 ====================
        private void AlarmTimer_Tick(object sender, EventArgs e)
        {
            if (_currentAlarmList.Count == 0) return;

            // 直接切换到下一条，并自动循环
            _currentAlarmIndex = (_currentAlarmIndex + 1) % _currentAlarmList.Count;
            ShowCurrentAlarm();
        }

        private void ShowCurrentAlarm()
        {
            if (_currentAlarmList.Count == 0 || _currentAlarmIndex >= _currentAlarmList.Count)
            {
                CurrentAlarm = null;
                CurrentAlarmColor = Brushes.Transparent;
                CurrentAlarmPriority = null;
                return;
            }

            var alarm = _currentAlarmList.Count > 0 && _currentAlarmIndex < _currentAlarmList.Count
            ? _currentAlarmList[_currentAlarmIndex] : null;

            if (alarm == null)
            {
                CurrentAlarm = null;
                CurrentAlarmColor = Brushes.Transparent;
                CurrentAlarmPriority = null;
                if (_previousPriority != null)
                {
                    AlarmPriorityChanged?.Invoke(null);
                    _previousPriority = null;
                }
                return;
            }

            CurrentAlarm = alarm.Name;
            CurrentAlarmPriority = alarm.Priority;
            CurrentAlarmColor = alarm.Priority switch
            {
                AlarmPriority.High => new SolidColorBrush(Colors.Red),
                AlarmPriority.Medium => new SolidColorBrush(Colors.Yellow),
                _ => Brushes.White
            };

            if (_previousPriority != alarm.Priority)
            {
                AlarmPriorityChanged?.Invoke(alarm.Priority);
                _previousPriority = alarm.Priority;
            }
        }

        // ==================== 离线状态控制 ====================
        public void SetOffline(bool offline)
        {
            IsOffline = offline;
            if (offline)
            {
                // 离线时清除所有报警，停止闪烁
                WaringInform = string.Empty;  // 或 null
            }
        }

        // ==================== JSON 数据更新 ====================
        public void UpdateFromJsonData(string jsonData)
        {
            try
            {
                var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonData);
                var root = jsonDoc.RootElement;

                // 1. 处理离线标记
                if (root.TryGetProperty("online", out var statusElement))
                {
                    string status = statusElement.GetString();
                    if (status == "false")
                    {
                        SetOffline(true);
                        return;
                    }
                    else if (status == "true")
                    {
                        SetOffline(false);
                    }
                }



                // 2. 判断数据类型
                //if (root.TryGetProperty("dataType", out var dataTypeElement))
                //{
                //    string dataType = dataTypeElement.GetString();
                //    if (dataType == "waveform")
                //    {
                //        ParseWaveformData(root);
                //        return;
                //    }
                //}

                // 2. 先尝试解析波形数据（无论是否有 dataType 字段）
                ParseWaveformData(root);  // 方法内部会检查字段是否存在，无字段时不做任何事


                // 3. 解析生理参数
                bool hasValidData = false;


                // 解析病人信息（如果存在）
                if (root.TryGetProperty("patientinform", out var patientElement) && patientElement.ValueKind == JsonValueKind.Object)
                {
                    UpdatePatientInfo(patientElement);
                    hasValidData = true;
                }

                if (root.TryGetProperty("HR", out var hrElement))
                {
                    HeartRate = hrElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("SPO2", out var spo2Element))
                {
                    BloodOxygen = spo2Element.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("RESP", out var respElement))
                {
                    RespirationRate = respElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("NIBP_SYS", out var sysElement) &&
                    root.TryGetProperty("NIBP_DIA", out var diaElement))
                {
                    string sys = sysElement.GetRawText().Trim('"');
                    string dia = diaElement.GetRawText().Trim('"');
                    BloodPressure = $"{sys}/{dia}";
                    hasValidData = true;
                }
                if (root.TryGetProperty("BOX_TEMP", out var boxTempElement))
                {
                    BoxTemp = boxTempElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("TEMP_T1", out var t1Element))
                {
                    SkinTemp1 = t1Element.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("TEMP_T2", out var t2Element))
                {
                    SkinTemp2 = t2Element.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("HUMIDITY", out var humidityElement))
                {
                    Humidity = humidityElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("OXYGEN_CONC", out var oxygenConcElement))
                {
                    O2 = oxygenConcElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("WEIGHT", out var weightElement))
                {
                    Weight = weightElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("PI", out var piElement))
                {
                    PI = piElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("PR", out var prElement))
                {
                    PR = prElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("PVI", out var pviElement))
                {
                    PV1 = pviElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("SpHb", out var sphbElement))
                {
                    SpHb = sphbElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("SpOC", out var spocElement))
                {
                    SpOC = spocElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("SpMet", out var spmetElement))
                {
                    SpMet = spmetElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("SpCO", out var spcoElement))
                {
                    SpCO = spcoElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("WorkMode", out var workModeElement))
                {
                    WorkMode = workModeElement.GetString().Trim('"');
                    hasValidData = true;
                }
                if (root.TryGetProperty("WorkModePercent", out var percentElement))
                {
                    if (percentElement.ValueKind == JsonValueKind.Number)
                    {
                        WorkModePercent = percentElement.GetInt32();
                        hasValidData = true;
                    }
                    else if (percentElement.ValueKind == JsonValueKind.String)
                    {
                        // 兼容字符串形式的数字
                        if (int.TryParse(percentElement.GetString(), out int val))
                        {
                            WorkModePercent = val;
                            hasValidData = true;
                        }
                    }
                }
                if (root.TryGetProperty("Qibo", out var qiboElement))
                {
                    Qibo = qiboElement.GetRawText().Trim('"');
                    hasValidData = true;
                }
                // 报警信息
                if (root.TryGetProperty("WarmInfomation", out var alarmElement))
                {
                    var alarmStr = alarmElement.GetString();
                    var typesStr = root.TryGetProperty("WarmTypes", out var typesElement)
                                   ? typesElement.GetString() : "";

                    var alarmNames = alarmStr.Split('，').Select(s => s.Trim())
                                             .Where(s => !string.IsNullOrEmpty(s)).ToList();
                    var alarmTypes = typesStr.Split('，').Select(s => s.Trim())
                                             .Where(s => !string.IsNullOrEmpty(s)).ToList();

                    for (int i = 0; i < alarmNames.Count; i++)
                    {
                        string type = i < alarmTypes.Count ? alarmTypes[i] : "生理报警";
                        var priority = AlarmPriorityMap.TryGetValue(alarmNames[i], out var p) ? p : AlarmPriority.Medium;
                        string level = priority switch
                        {
                            AlarmPriority.High => "紧急",
                            AlarmPriority.Medium => "警告",
                            AlarmPriority.Low => "轻微",
                            _ => "警告"
                        };

                        var log = new AlarmLogItem
                        {
                            BedNumber = BedNumber,
                            PatientName = PatientNameForDisplay,
                            AlarmContent = alarmNames[i],
                            AlarmType = type,
                            AlarmLevel = level,
                            AlarmTime = DateTime.Now,
                            DeviceModel = DeviceType,  // 或 DeviceName
                            ParameterName = null,      // 暂不填充
                            CurrentValue = null
                        };
                        AlarmLogService.Instance.AddLog(log);
                    }

                    WaringInform = alarmStr;
                    hasValidData = true;
                }

                if (hasValidData && IsOffline)
                    SetOffline(false);

                OnPropertyChanged(nameof(PatientNameForDisplay));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 更新床位 {BedNumber} 数据时出错: {ex.Message}");
            }
        }

        private void ParseWaveformData(JsonElement root)
        {
            if (root.TryGetProperty("Wave_II", out var w)) Hr_Ⅱ = w.GetString();
            if (root.TryGetProperty("Wave_RA", out w)) Hr_RA = w.GetString();
            if (root.TryGetProperty("Wave_LA", out w)) Hr_LA = w.GetString();
            if (root.TryGetProperty("Wave_RL", out w)) Hr_RL = w.GetString();
            if (root.TryGetProperty("Wave_LL", out w)) Hr_LL = w.GetString();
            if (root.TryGetProperty("Wave_V", out w)) Hr_Ⅴ = w.GetString();
            if (root.TryGetProperty("Wave_Pletch", out w)) PletchWaveData = w.GetString();
            if (root.TryGetProperty("Wave_EtCO2", out w)) Ecto2WaveData = w.GetString();
        }

        private void UpdatePatientInfo(JsonElement patientElement)
        {
            // 辅助方法：读取字符串字段，为空则不更新
            void UpdateIfNotEmpty(ref string targetField, string propertyName)
            {
                if (patientElement.TryGetProperty(propertyName, out var element) &&
                    element.ValueKind == JsonValueKind.String)
                {
                    string value = element.GetString();
                    if (!string.IsNullOrWhiteSpace(value) && value != targetField)
                    {
                        targetField = value;
                        // 注意：此处需手动触发 PropertyChanged，但因为我们直接设置字段，
                        // 为了保持一致性，应通过属性 setter 调用 OnPropertyChanged。
                        // 所以改为直接设置属性。
                    }
                }
            }

            // 逐个更新属性（只有存在且不同才更新）
            string newVal;

            // PatientId
            if (patientElement.TryGetProperty("patientid", out var pid) && pid.ValueKind == JsonValueKind.String)
            {
                newVal = pid.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != _patientId.ToString())
                {
                    if (int.TryParse(newVal, out int id))
                        PatientId = id;  // 属性赋值触发通知
                }
            }

            // PatientName
            if (patientElement.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
            {
                newVal = nameEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != PatientName)
                    PatientName = newVal;
            }

            // Sex
            if (patientElement.TryGetProperty("sex", out var sexEl) && sexEl.ValueKind == JsonValueKind.String)
            {
                newVal = sexEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != Sex)
                    Sex = newVal;
            }

            // BirthYear
            if (patientElement.TryGetProperty("birthyear", out var byEl) && byEl.ValueKind == JsonValueKind.String)
            {
                newVal = byEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != BirthYear)
                    BirthYear = newVal;
            }

            // BirthMonth
            if (patientElement.TryGetProperty("birthmonth", out var bmEl) && bmEl.ValueKind == JsonValueKind.String)
            {
                newVal = bmEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != BirthMonth)
                    BirthMonth = newVal;
            }

            // BirthDay
            if (patientElement.TryGetProperty("birthday", out var bdEl) && bdEl.ValueKind == JsonValueKind.String)
            {
                newVal = bdEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != BirthDay)
                    BirthDay = newVal;
            }

            // GestationalAge (胎龄) - PatientTaiLing
            if (patientElement.TryGetProperty("gestationalage", out var gaEl) && gaEl.ValueKind == JsonValueKind.String)
            {
                newVal = gaEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != PatientTaiLing)
                    PatientTaiLing = newVal;
            }

            // Weight (体重)
            if (patientElement.TryGetProperty("weight", out var wEl) && wEl.ValueKind == JsonValueKind.String)
            {
                newVal = wEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != Weight)
                    Weight = newVal;
            }

            // Height (身高)
            if (patientElement.TryGetProperty("height", out var hEl) && hEl.ValueKind == JsonValueKind.String)
            {
                newVal = hEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != Height)
                    Height = newVal;
            }

            // AgeDays (日龄) - PatientOld
            if (patientElement.TryGetProperty("agedays", out var adEl) && adEl.ValueKind == JsonValueKind.String)
            {
                newVal = adEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != PatientOld)
                    PatientOld = newVal;  // 或 AgeDays = newVal;
            }

            // BloodType
            if (patientElement.TryGetProperty("bloodtype", out var btEl) && btEl.ValueKind == JsonValueKind.String)
            {
                newVal = btEl.GetString();
                if (!string.IsNullOrWhiteSpace(newVal) && newVal != BloodType)
                    BloodType = newVal;
            }
        }

        // ==================== 资源清理 ====================
        public void CleanupWarminfo()
        {
            _alarmTimer?.Stop();
            _alarmTimer = null;
        }

        // ==================== INotifyPropertyChanged ====================
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}