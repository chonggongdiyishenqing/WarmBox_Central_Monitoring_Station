using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.ViewModel;

namespace WarmBox_Central_Monitoring_Station.View
{
    public partial class BedIndex : UserControl
    {
        private Dictionary<string, SimpleWaveformRenderer> _renderers = new Dictionary<string, SimpleWaveformRenderer>();
        private BedViewModel _currentBed;

        public BedIndex()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
            this.Unloaded += OnUnloaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_currentBed != null)
            {
                UnsubscribeFromEvents(_currentBed);
                CleanupRenderers();
            }

            BedViewModel newBed = null;
            if (e.NewValue is BedViewModel bedVm)
                newBed = bedVm;
            else if (e.NewValue is BedIndexViewModel indexVm)
                newBed = indexVm.Bed;

            if (newBed != null && newBed.IsBound)
            {
                _currentBed = newBed;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    InitializeWaveforms();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                SubscribeToEvents(newBed);
            }
        }

        private void InitializeWaveforms()
        {
            if (Wave_Ⅰ == null) return;

            _renderers = new Dictionary<string, SimpleWaveformRenderer>
            {
                ["Ⅰ"] = CreateRenderer(Wave_Ⅰ, Brushes.LimeGreen, -150, 600, 1.0, 0.2),
                ["Ⅱ"] = CreateRenderer(Wave_Ⅱ, Brushes.LimeGreen, -300, 800, 1.0, 0.2),
                ["Ⅲ"] = CreateRenderer(Wave_Ⅲ, Brushes.LimeGreen, -200, 300, 1.0, 0.2),
                // aVR 和 V1 使用动态范围，自动适应正向/反向波形
                ["aVR"] = CreateRenderer(Wave_AVR, Brushes.LimeGreen, null, null, 1.0, 0.2),
                ["aVL"] = CreateRenderer(Wave_AVL, Brushes.LimeGreen, -100, 200, 1.0, 0.2),
                ["aVF"] = CreateRenderer(Wave_AVF, Brushes.LimeGreen, -250, 550, 1.0, 0.2),
                ["V1"] = CreateRenderer(Wave_V1, Brushes.LimeGreen, null, null, 1.0, 0.2),
                ["Pletch"] = CreateRenderer(Wave_Pletch, Brushes.Cyan, -30000, 30000, 1.0, 0.6),
                ["EtCO2"] = CreateRenderer(Wave_EtCO2, Brushes.Orange, 200, 400, 1.0, 0.1),
                ["Resp"] = CreateRenderer(Wave_Resp, Brushes.OrangeRed, -200, 200, 1.0, 0.3)
            };

            foreach (var renderer in _renderers.Values)
                renderer.Start();
        }

        private SimpleWaveformRenderer CreateRenderer(Canvas canvas, Brush stroke, double? minY, double? maxY, double st, double sp, int intervalMs = 6)
        {
            return new SimpleWaveformRenderer(
                canvas: canvas,
                stroke: stroke,
                strokeThickness: st,
                pointSpacing: sp,
                fixedMinY: minY,
                fixedMaxY: maxY,
                intervalMs: intervalMs,   // 传入参数
                addInitialPoints: false
            );
        }

        private void SubscribeToEvents(BedViewModel bed)
        {
            bed.Hr1WaveDataUpdated += OnHr1Data;
            bed.Hr2WaveDataUpdated += OnHr2Data;
            bed.Hr3WaveDataUpdated += OnHr3Data;
            bed.HrAVRWaveDataUpdated += OnHrAVRData;
            bed.HrAVLWaveDataUpdated += OnHrAVLData;
            bed.HrAVFWaveDataUpdated += OnHrAVFData;
            bed.HrV1WaveDataUpdated += OnHrV1ata;
            bed.PletchWaveDataUpdated += OnPletchData;
            bed.Etco2WaveDataUpdated += OnEtco2Data;
            bed.RespWaveDataUpdated += OnRespData;
        }
        private void UnsubscribeFromEvents(BedViewModel bed)
        {
            if (bed == null) return;
            bed.Hr1WaveDataUpdated -= OnHr1Data;
            bed.Hr2WaveDataUpdated -= OnHr2Data;
            bed.Hr3WaveDataUpdated -= OnHr3Data;
            bed.HrAVRWaveDataUpdated -= OnHrAVRData;
            bed.HrAVLWaveDataUpdated -= OnHrAVLData;
            bed.HrAVFWaveDataUpdated -= OnHrAVFData;
            bed.HrV1WaveDataUpdated -= OnHrV1ata;
            bed.RespWaveDataUpdated -= OnRespData;
            bed.PletchWaveDataUpdated -= OnPletchData;
            bed.Etco2WaveDataUpdated -= OnEtco2Data;
            bed.PropertyChanged -= OnBedPropertyChanged;
        }

        private void OnBedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 状态改变时无需额外操作
        }

        private void OnHr1Data(string data) => UpdateWaveform("Ⅰ", data);
        private void OnHr2Data(string data) => UpdateWaveform("Ⅱ", data);
        private void OnHr3Data(string data) => UpdateWaveform("Ⅲ", data);
        private void OnHrAVRData(string data) => UpdateWaveform("aVR", data);
        private void OnHrAVLData(string data) => UpdateWaveform("aVL", data);
        private void OnHrAVFData(string data) => UpdateWaveform("aVF", data);

        private void OnHrV1ata(string data) => UpdateWaveform("V1", data);

        private void OnRespData(string data) => UpdateWaveform("Resp", data);
        private void OnPletchData(string data) => UpdateWaveform("Pletch", data);
        private void OnEtco2Data(string data) => UpdateWaveform("EtCO2", data);

        private void UpdateWaveform(string name, string csvData)
        {           
            if (_currentBed != null && _currentBed.IsWaveformPaused)
                return;

            Dispatcher.Invoke(() =>
            {
                if (_renderers == null) return;
                if (_renderers.TryGetValue(name, out var renderer))
                    renderer?.AddCsvData(csvData);
            });
        }

        private void CleanupRenderers()
        {
            if (_renderers != null)
            {
                foreach (var renderer in _renderers.Values)
                    renderer.Dispose();
                _renderers.Clear();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeFromEvents(_currentBed);
            CleanupRenderers();
        }
    }
}