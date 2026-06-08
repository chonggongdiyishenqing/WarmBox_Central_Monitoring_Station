using System;
using System.Collections.Generic;
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

namespace WarmBox_Central_Monitoring_Station.ViewModel
{
    public class WarmBoxViewModel : INotifyPropertyChanged
    {
        private readonly IServiceDataService _dataService;

        // 数据
        private ObservableCollection<BedViewModel> _allBeds;
        private ObservableCollection<BedViewModel> _displayBeds = new ObservableCollection<BedViewModel>();
        private ObservableCollection<BedViewModel> _detailModeBeds = new ObservableCollection<BedViewModel>();
        private BedDetailViewModel _selectedBedDetailVM;
        public BedDetailViewModel SelectedBedDetailVM
        {
            get => _selectedBedDetailVM;
            set
            {
                _selectedBedDetailVM = value;
                OnPropertyChanged();
            }
        }

        // 状态
        public int TotalBeds => AllBeds?.Count ?? 0;
        private int _criticalAlarmCount;
        public int CriticalAlarmCount
        {
            get => _criticalAlarmCount;
            set { _criticalAlarmCount = value; OnPropertyChanged(); }
        }

        private int _warningAlarmCount;
        public int WarningAlarmCount
        {
            get => _warningAlarmCount;
            set { _warningAlarmCount = value; OnPropertyChanged(); }
        }
        private int _currentViewMode = 16;
        private int _currentPage = 1;
        private bool _isDetailMode;
        public bool IsDetailMode
        {
            get => _isDetailMode;
            set
            {
                if (_isDetailMode != value)
                {
                    StopAllWaveforms();
                    _isDetailMode = value;
                    OnPropertyChanged();
                    UpdateDisplayBeds();
                    StartWaveformsForCurrentDisplay();
                    DetailModeChanged?.Invoke(value);
                    if (!value)
                    {
                        SelectedBed = null;
                    }
                }
            }
        }
        private BedViewModel _selectedBed;
        private bool _isPrivacyModeEnabled;

        // 命令
        public ICommand ShowDetailCommand { get; }
        public ICommand CloseDetailCommand { get; }

        // 事件
        public event Action<bool> DetailModeChanged;

        public WarmBoxViewModel(IServiceDataService dataService)
        {
            _dataService = dataService;
            BedConfigEventPublisher.BedConfigChanged += OnBedConfigChanged;
            // 关闭详情模式的命令
            CloseDetailCommand = new RelayCommand(() =>
            {
                IsDetailMode = false;
                SelectedBed = null;
            });

            // 显示详情命令：创建 BedDetailViewModel 并传入关闭回调
            ShowDetailCommand = new RelayCommand<BedViewModel>(bed =>
            {
                SelectedBed = bed;                     // 关键：更新选中床位，触发高亮
                SelectedBedDetailVM = new BedDetailViewModel(bed, () => CloseDetailCommand.Execute(null));
                if (!IsDetailMode)                     // 如果尚未处于详情模式，再进入
                    IsDetailMode = true;
            });

            LoadBedsAsync();
        }

        private void OnBedConfigChanged()
        {
            // 确保在 UI 线程上执行，避免跨线程问题
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                 LoadBedsAsync();
            });
        }

        // ========== 公共属性 ==========
        public ObservableCollection<BedViewModel> AllBeds
        {
            get => _allBeds;
            private set
            {
                if (_allBeds != null)
                {
                    foreach (var bed in _allBeds)
                        UnsubscribeBedAlarm(bed);
                }

                _allBeds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalBeds)); 

                if (_allBeds != null)
                {
                    foreach (var bed in _allBeds)
                        SubscribeBedAlarm(bed);
                }

                RecalculateAlarmCounts();
                UpdateDisplayBeds();
                OnPropertyChanged(nameof(DetailModeRows));
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(Pages));
            }
        }

        public ObservableCollection<BedViewModel> DisplayBeds
        {
            get => _displayBeds;
            private set { _displayBeds = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BedViewModel> DetailModeBeds
        {
            get => _detailModeBeds;
            private set { _detailModeBeds = value; OnPropertyChanged(); }
        }

        public int CurrentViewMode
        {
            get => _currentViewMode;
            set
            {
                if (_currentViewMode != value)
                {
                    _currentViewMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GridColumns));
                    OnPropertyChanged(nameof(GridRows));
                    UpdateDisplayBeds();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    UpdateDisplayBeds();
                }
            }
        }

        public BedViewModel SelectedBed
        {
            get => _selectedBed;
            set
            {
                if (_selectedBed != value)
                {
                    if (_selectedBed != null)
                        _selectedBed.IsSelected = false;
                    _selectedBed = value;
                    if (_selectedBed != null)
                        _selectedBed.IsSelected = true;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPrivacyModeEnabled
        {
            get => _isPrivacyModeEnabled;
            set
            {
                _isPrivacyModeEnabled = value;
                OnPropertyChanged();
                foreach (var bed in AllBeds ?? Enumerable.Empty<BedViewModel>())
                    bed.IsPatientNameVisible = !value;
            }
        }

        // ========== 布局相关属性 ==========
        public int GridColumns => CurrentViewMode switch
        {
            16 => 4,
            8 => 4,
            _ => 4
        };

        public int GridRows => CurrentViewMode switch
        {
            16 => 4,
            8 => 2,
            _ => 4
        };

        public int DetailModeRows => (int)Math.Ceiling((double)(AllBeds?.Count ?? 0) / 2);

        public int TotalPages
        {
            get
            {
                if (AllBeds == null || AllBeds.Count == 0) return 1;
                return (int)Math.Ceiling((double)AllBeds.Count / CurrentViewMode);
            }
        }

        public IEnumerable<int> Pages => Enumerable.Range(1, TotalPages);

        // ========== 核心方法 ==========
        private void UpdateDisplayBeds()
        {
            if (AllBeds == null) return;

            if (IsDetailMode)
            {
                DetailModeBeds = new ObservableCollection<BedViewModel>(AllBeds);
                OnPropertyChanged(nameof(DetailModeRows));
            }
            else
            {
                int bedsPerPage = CurrentViewMode;
                int totalPages = TotalPages;
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > totalPages && totalPages > 0) CurrentPage = totalPages;

                int startIndex = (CurrentPage - 1) * bedsPerPage;
                var pageBeds = AllBeds.Skip(startIndex).Take(bedsPerPage).ToList();
                DisplayBeds = new ObservableCollection<BedViewModel>(pageBeds);
            }
        }

        private async void LoadBedsAsync()
        {
            try
            {
                var devices = await _dataService.GetBoundDevicesAsync();
                var beds = new ObservableCollection<BedViewModel>();
                foreach (var dev in devices)
                {
                    beds.Add(new BedViewModel
                    {
                        BedNumber = dev.PatientChuangwei,
                        DeviceIp = dev.DeviceIp,
                        DeviceType = dev.DeviceType,
                        DeviceName = dev.DeviceName,
                        PatientName = dev.PatientName ?? "--",
                        Sex = dev.Sex,
                        PatientOld = dev.Patient_dayold,
                        PatientTaiLing = dev.Patient_GestationalAge,
                        IsBound = true,
                    });
             
                }
                AllBeds = beds;
            }
            catch (Exception)
            {
                AllBeds = new ObservableCollection<BedViewModel>();
            }
        }

        public void UpdateViewMode(int mode)
        {
            if (IsDetailMode)
                IsDetailMode = false;
            CurrentViewMode = mode;
        }

        public void UpdatePage(int page)
        {
            if (IsDetailMode)
                IsDetailMode = false;
            CurrentPage = page;
        }

        // 订阅一个床位的报警事件
        private void SubscribeBedAlarm(BedViewModel bed)
        {
            bed.AlarmPriorityChanged += OnBedAlarmPriorityChanged;
        }

        // 取消订阅
        private void UnsubscribeBedAlarm(BedViewModel bed)
        {
            bed.AlarmPriorityChanged -= OnBedAlarmPriorityChanged;
        }

        // 报警变化回调
        private void OnBedAlarmPriorityChanged(AlarmPriority? priority)
        {
            Application.Current.Dispatcher.Invoke(() => RecalculateAlarmCounts());
        }

        // 重新计算紧急和警告报警数量
        private void RecalculateAlarmCounts()
        {
            if (_allBeds == null)
            {
                CriticalAlarmCount = 0;
                WarningAlarmCount = 0;
                return;
            }
            CriticalAlarmCount = _allBeds.Count(b => b.CurrentAlarmPriority == AlarmPriority.High);
            WarningAlarmCount = _allBeds.Count(b => b.CurrentAlarmPriority == AlarmPriority.Medium);
        }

        private void StopAllWaveforms() { }
        private void StartWaveformsForCurrentDisplay() { }

        // ========== INotifyPropertyChanged ==========
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}