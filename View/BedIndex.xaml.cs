using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            if (Wave_II == null) return;

            _renderers = new Dictionary<string, SimpleWaveformRenderer>
            {
                ["II"] = CreateRenderer(Wave_II, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["RA"] = CreateRenderer(Wave_RA, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["LA"] = CreateRenderer(Wave_LA, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["RL"] = CreateRenderer(Wave_RL, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["LL"] = CreateRenderer(Wave_LL, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["V"] = CreateRenderer(Wave_V, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["Pletch"] = CreateRenderer(Wave_Pletch, Brushes.Cyan, 300, 700, 1.0, 0.5),
                ["EtCO2"] = CreateRenderer(Wave_EtCO2, Brushes.Orange, 200, 400, 1.0, 0.11)
            };

            foreach (var renderer in _renderers.Values)
                renderer.Start();
        }

        private SimpleWaveformRenderer CreateRenderer(Canvas canvas, Brush stroke, double minY, double maxY, double st, double sp)
        {
            return new SimpleWaveformRenderer(
                canvas: canvas,
                stroke: stroke,
                strokeThickness: st,
                pointSpacing: sp,
                fixedMinY: minY,
                fixedMaxY: maxY,
                intervalMs: 6,
                addInitialPoints: false
            );
        }

        private void SubscribeToEvents(BedViewModel bed)
        {
            bed.hr2WaveDataUpdated += OnHr2Data;
            bed.hrraWaveDataUpdated += OnHrRaData;
            bed.hrlaWaveDataUpdated += OnHrLaData;
            bed.hrrlWaveDataUpdated += OnHrRlData;
            bed.hrllWaveDataUpdated += OnHrLlData;
            bed.hr5WaveDataUpdated += OnHr5Data;
            bed.PletchWaveDataUpdated += OnPletchData;
            bed.Etco2WaveDataUpdated += OnEtco2Data;
            bed.PropertyChanged += OnBedPropertyChanged;
        }

        private void UnsubscribeFromEvents(BedViewModel bed)
        {
            if (bed == null) return;
            bed.hr2WaveDataUpdated -= OnHr2Data;
            bed.hrraWaveDataUpdated -= OnHrRaData;
            bed.hrlaWaveDataUpdated -= OnHrLaData;
            bed.hrrlWaveDataUpdated -= OnHrRlData;
            bed.hrllWaveDataUpdated -= OnHrLlData;
            bed.hr5WaveDataUpdated -= OnHr5Data;
            bed.PletchWaveDataUpdated -= OnPletchData;
            bed.Etco2WaveDataUpdated -= OnEtco2Data;
            bed.PropertyChanged -= OnBedPropertyChanged;
        }

        private void OnBedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 状态改变时无需额外操作
        }

        private void OnHr2Data(string data) => UpdateWaveform("II", data);
        private void OnHrRaData(string data) => UpdateWaveform("RA", data);
        private void OnHrLaData(string data) => UpdateWaveform("LA", data);
        private void OnHrRlData(string data) => UpdateWaveform("RL", data);
        private void OnHrLlData(string data) => UpdateWaveform("LL", data);
        private void OnHr5Data(string data) => UpdateWaveform("V", data);
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