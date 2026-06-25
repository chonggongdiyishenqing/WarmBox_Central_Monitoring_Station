using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Services;
using WarmBox_Central_Monitoring_Station.View;
using WarmBox_Central_Monitoring_Station.View.SetIndexs;
using static WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs.DeviceWeihuViewModel;

namespace WarmBox_Central_Monitoring_Station.ViewModel
{
    public class IndexViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProcessService _processService;
        private WarmBoxViewModel _warmboxviewmodel;
        public WarmBoxViewModel WarmBoxViewModel => _warmboxviewmodel;

        public event Action<int> ViewModeChanged;
        public event Action<int> PageChanged;



        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }

        private string _currentTime;
        private DispatcherTimer _timer;
        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged(nameof(CurrentTime));
            }
        }

        // 当前页码
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                    PageChanged?.Invoke(value);
                }
            }
        }

        private bool _isPrivacyModeEnabled = false;
        public bool IsPrivacyModeEnabled
        {
            get => _isPrivacyModeEnabled;
            set
            {
                _isPrivacyModeEnabled = value;
                OnPropertyChanged(nameof(IsPrivacyModeEnabled));             
                if (_warmboxviewmodel != null)
                {
                    _warmboxviewmodel.IsPrivacyModeEnabled = value;
                }
            }
        }

        private int _selectedViewMode = 16; // 默认16视图
        public int SelectedViewMode
        {
            get => _selectedViewMode;
            set
            {
                if (_selectedViewMode != value)
                {
                    _selectedViewMode = value;
                    OnPropertyChanged();
                    CurrentPage = 1;
                    ViewModeChanged?.Invoke(value);
                }
            }
        }

        // 分页按钮可见性（详情模式下隐藏）
        private bool _isPageNavigationVisible = true;
        public bool IsPageNavigationVisible
        {
            get => _isPageNavigationVisible;
            set
            {
                if (_isPageNavigationVisible != value)
                {
                    _isPageNavigationVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        // 视图切换命令
        public ICommand Select16ViewCommand { get; }
        public ICommand Select8ViewCommand { get; }
        public ICommand SelectPageCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand TogglePatientNameCommand { get; }


        public RelayCommand OpenSetBaojinCommand { get; private set; }
        public RelayCommand OpenSetIndexCommand { get; private set; }

       

        public IndexViewModel(IServiceProvider serviceProvider, IProcessService processService)
        {
            _serviceProvider = serviceProvider;
            _processService = processService;
            _warmboxviewmodel = new WarmBoxViewModel(serviceProvider.GetService<IServiceDataService>());

            // 订阅详情模式变化事件，控制分页按钮可见性
            _warmboxviewmodel.DetailModeChanged += OnDetailModeChanged;

            CurrentView = new WarmBoxView { DataContext = _warmboxviewmodel };

            // 订阅事件以传递视图模式和页码变化
            ViewModeChanged += mode => _warmboxviewmodel.UpdateViewMode(mode);
            PageChanged += page => _warmboxviewmodel.UpdatePage(page);

            // 初始同步
            _warmboxviewmodel.UpdateViewMode(SelectedViewMode);
            _warmboxviewmodel.UpdatePage(CurrentPage);

            Select16ViewCommand = new RelayCommand(() => SelectedViewMode = 16);
            Select8ViewCommand = new RelayCommand(() => SelectedViewMode = 8);
            CloseCommand = new RelayCommand(_ => ExecuteClose());
            SelectPageCommand = new RelayCommand<int>(page => CurrentPage = page);
            OpenSetBaojinCommand = new RelayCommand(OpenSetBaojin);
            OpenSetIndexCommand = new RelayCommand(OpenSetIndex);
            TogglePatientNameCommand = new RelayCommand(() =>
            {
                IsPrivacyModeEnabled = !IsPrivacyModeEnabled;
                // PrivacyButtonContent 会自动更新（因为它依赖于 IsPrivacyModeEnabled）
            });
            StartClock();

        }

        private void OpenSetBaojin()
        {
            var views= App.ServiceProvider.GetRequiredService<SetBaojinListView>();
            views.DataContext = new SetBaojinListViewModel();
            views.Owner = Application.Current.MainWindow;
            views.ShowDialog();

        }

        private void OpenSetIndex()
        {
            var views = App.ServiceProvider.GetRequiredService<SetIndex>();
            views.DataContext = App.ServiceProvider.GetRequiredService<SetIndexViewModel>();
            views.Owner = Application.Current.MainWindow;
            views.ShowDialog();
        }

        private void OnDetailModeChanged(bool isDetailMode)
        {
            // 详情模式下隐藏分页按钮，正常模式下显示
            IsPageNavigationVisible = !isDetailMode;
        }

        private void StartClock()
        {
            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void StopClock()
        {
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
        }

        private async void ExecuteClose()
        {
            var result = MessageBox.Show("确定要退出系统吗？", "确认退出",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                StopClock();
                //if (_processService != null)
                //{
                //    await _processService.StopProcessAsync("WarmBox_Date_AcceptSend");
                //}
                Application.Current.Shutdown();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}