using HandyControl.Controls;
using HandyControl.Tools.Extension;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Services;
using WarmBox_Central_Monitoring_Station.View;
using WarmBox_Central_Monitoring_Station.View.BedDetialPopupUsercontroles;

namespace WarmBox_Central_Monitoring_Station.ViewModel
{
    public class BedDetailViewModel : INotifyPropertyChanged
    {
        private readonly BedViewModel _bed;
        private readonly Action _closeCallback;
        private object _currentDetailView;
        public event Action PrintTriggered;




        // 构造函数增加 closeCallback 参数
        public BedDetailViewModel(BedViewModel bed, Action closeCallback)
        {
            _bed = bed;
            _closeCallback = closeCallback;

            // 初始化命令
            ShowWaveformCommand = new RelayCommand(ShowWaveform);
            ShowTrendReviewCommand = new RelayCommand(ShowTrendReview);
            ShowCameraCommand = new RelayCommand(ShowCamera);
            CloseCommand = new RelayCommand(() => _closeCallback?.Invoke());
            ShowBedIndexCommand = new RelayCommand(ShowBedIndex);
            PrintCommand = new RelayCommand(_ => PrintTriggered?.Invoke());
            // 默认显示波形视图（后续替换为实际视图）
            // 由于 WaveformView 还未创建，先用一个简单文本占位
            ShowBedIndex();
         
        }

        public BedViewModel Bed => _bed;

        public object CurrentDetailView
        {
            get => _currentDetailView;
            set
            {
                _currentDetailView = value;
                OnPropertyChanged();
            }
        }

        public ICommand PrintCommand { get; }
        public ICommand ShowWaveformCommand { get; }
        public ICommand ShowTrendReviewCommand { get; }

        public ICommand ShowBedIndexCommand { get; }
        public ICommand ShowCameraCommand { get; }
        public ICommand CloseCommand { get; }

        private void ShowWaveform()
        {
            // TODO: 替换为真正的 WaveformView
            CurrentDetailView = new System.Windows.Controls.TextBlock
            {
                Text = "波形视图",
                Foreground = System.Windows.Media.Brushes.White
            };
        }

        private void ShowTrendReview()
        {
            // 1. 从容器获取视图实例
            var trendView = App.ServiceProvider.GetRequiredService<_24hourDateView>();

            // 2. 从容器获取 IServiceDataService 依赖
            var serviceDataService = App.ServiceProvider.GetRequiredService<IServiceDataService>();

            // 3. 手动创建 ViewModel，并传入返回回调（回到 BedIndex）
            var trendViewModel = new _24hourDateViewModel(serviceDataService,_bed, () =>
            {
                // 回调：显示 BedIndex 视图
                ShowBedIndex();
            });

            // 4. 设置 DataContext
            trendView.DataContext = trendViewModel;

            // 5. 显示在 CurrentDetailView 中
            CurrentDetailView = trendView;
        }

        //private void ShowBedIndex()
        //{
        //    // TODO: 替换为真正的 TrendReviewView
        //    var views = App.ServiceProvider.GetRequiredService<BedIndex>();
        //    views.DataContext = App.ServiceProvider.GetRequiredService<BedIndexViewModel>();
        //    CurrentDetailView = views;

        //}

        private void ShowBedIndex()
        {
            var bedIndexView = App.ServiceProvider.GetRequiredService<BedIndex>();
            var bedIndexVM = new BedIndexViewModel(_bed);  // 传入当前床位
            bedIndexView.DataContext = bedIndexVM;
            CurrentDetailView = bedIndexView;
        }
        private void ShowCamera()
        {
            var cameraView = new CameraConnectingView();
            var window = new System.Windows.Window
            {
                Title = "摄像头",
                Content = cameraView,
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,          // 无边框（配合你自定义的标题栏）
                Background = System.Windows.Media.Brushes.Transparent
            };
            window.ShowDialog();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
