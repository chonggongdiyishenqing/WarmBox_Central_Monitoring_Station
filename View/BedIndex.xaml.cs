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
                ["Ⅰ"] = CreateRenderer(Wave_Ⅰ, Brushes.LimeGreen, -100, 500, 1.0, 0.2),
                ["Ⅱ"] = CreateRenderer(Wave_Ⅱ, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["Ⅲ"] = CreateRenderer(Wave_Ⅲ, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["aVR"] = CreateRenderer(Wave_AVR, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["aVL"] = CreateRenderer(Wave_AVL, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["aVF"] = CreateRenderer(Wave_AVF, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["V1"] = CreateRenderer(Wave_V1, Brushes.LimeGreen, -700, 1100, 1.0, 0.2),
                ["Pletch"] = CreateRenderer(Wave_Pletch, Brushes.Cyan, 0, 1000, 1.0, 0.5),
                ["EtCO2"] = CreateRenderer(Wave_EtCO2, Brushes.Orange, 200, 400, 1.0, 0.11),
                ["Resp"] = CreateRenderer(Wave_Resp, Brushes.OrangeRed, 0, 1000, 1.0, 0.5) // 范围根据实际调整
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
            bed.Hr1WaveDataUpdated += data => UpdateWaveform("Ⅰ", data);
            bed.Hr2WaveDataUpdated += data => UpdateWaveform("Ⅱ", data);
            bed.Hr3WaveDataUpdated += data => UpdateWaveform("Ⅲ", data);
            bed.HrAVRWaveDataUpdated += data => UpdateWaveform("aVR", data);
            bed.HrAVLWaveDataUpdated += data => UpdateWaveform("aVL", data);
            bed.HrAVFWaveDataUpdated += data => UpdateWaveform("aVF", data);
            bed.HrV1WaveDataUpdated += data => UpdateWaveform("V1", data);
            bed.PletchWaveDataUpdated += data => UpdateWaveform("Pletch", data);
            bed.Etco2WaveDataUpdated += data => UpdateWaveform("EtCO2", data);
            bed.RespWaveDataUpdated += data => UpdateWaveform("Resp", data);
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