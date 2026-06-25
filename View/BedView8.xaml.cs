using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.ViewModel;

namespace WarmBox_Central_Monitoring_Station.View
{
    /// <summary>
    /// BedView8.xaml 的交互逻辑
    /// </summary>
    public partial class BedView8 : UserControl
    {
        private SimpleWaveformRenderer _ecgRenderer;
        private SimpleWaveformRenderer _pletchRenderer;
        private BedViewModel _currentBed;

        public BedView8()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
            this.Unloaded += OnUnloaded;
            this.SizeChanged += OnSizeChanged;  // 监听大小变化
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is BedViewModel oldBed)
            {
                UnsubscribeFromEvents(oldBed);
                CleanupRenderers();
            }

            if (e.NewValue is BedViewModel newBed && newBed.IsBound)
            {
                _currentBed = newBed;
                // 延迟初始化，等待布局完成
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    InitializeWaveforms();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                SubscribeToEvents(newBed);
            }
        }

        private void InitializeWaveforms()
        {
            if (Wave_II == null || Wave_Pletch == null) return;

            // 确保 Canvas 已经获得实际宽度
            if (Wave_II.ActualWidth <= 0) return;

            _ecgRenderer = new SimpleWaveformRenderer(
                canvas: Wave_II,
                stroke: Brushes.LimeGreen,
                strokeThickness: 1.2,
                pointSpacing: 0.11,
                fixedMinY: -700,
                fixedMaxY: 1100,
                intervalMs: 6,
                addInitialPoints: false
            );

            _pletchRenderer = new SimpleWaveformRenderer(
                canvas: Wave_Pletch,
                stroke: Brushes.Cyan,
                strokeThickness: 1.2,
                pointSpacing: 0.5,
                fixedMinY: 300,
                fixedMaxY: 700,
                intervalMs: 6,
                addInitialPoints: false
            );

            _ecgRenderer.Start();
            _pletchRenderer.Start();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 当控件大小改变时，通知渲染器更新画布宽度
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                _ecgRenderer?.UpdateCanvasSize();
                _pletchRenderer?.UpdateCanvasSize();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SubscribeToEvents(BedViewModel bed)
        {
            bed.Hr2WaveDataUpdated += OnEcgData;
            bed.PletchWaveDataUpdated += OnPletchData;
        }

        private void UnsubscribeFromEvents(BedViewModel bed)
        {
            if (bed == null) return;
            bed.Hr2WaveDataUpdated -= OnEcgData;
            bed.PletchWaveDataUpdated -= OnPletchData;
        }

        private void OnEcgData(string csvData)
        {
            Dispatcher.Invoke(() => _ecgRenderer?.AddCsvData(csvData));
        }

        private void OnPletchData(string csvData)
        {
            Dispatcher.Invoke(() => _pletchRenderer?.AddCsvData(csvData));
        }

        private void CleanupRenderers()
        {
            _ecgRenderer?.Dispose();
            _pletchRenderer?.Dispose();
            _ecgRenderer = null;
            _pletchRenderer = null;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeFromEvents(_currentBed);
            CleanupRenderers();
        }
    }
}
