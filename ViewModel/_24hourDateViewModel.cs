using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
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
    public class DateTimePoint
    {
        public DateTime DateTime { get; set; }
        public double? Value { get; set; }
    }

    public class _24hourDateViewModel : INotifyPropertyChanged
    {
        private readonly IServiceDataService _serviceDataService;
        private readonly Action _onReturnToBedIndex;
        private readonly BedViewModel _bed;

        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }
        public ObservableCollection<ISeries> Series { get; set; } = new();

        private string _selectedParameter = "HR";
        public string SelectedParameter
        {
            get => _selectedParameter;
            set
            {
                if (_selectedParameter != value)
                {
                    _selectedParameter = value;
                    OnPropertyChanged();
                    _ = LoadDataAsync();
                }
            }
        }

        private int _selectedParameterIndex = 0;
        public int SelectedParameterIndex
        {
            get => _selectedParameterIndex;
            set
            {
                if (_selectedParameterIndex != value)
                {
                    _selectedParameterIndex = value;
                    SelectedParameter = value switch
                    {
                        0 => "HR",
                        1 => "SPO2",
                        2 => "RESP",
                        3 => "NIBP",
                        4 => "TEMP",
                        5 => "PI",
                        _ => "HR"
                    };
                }
            }
        }

        private DateTime? _selectedDate = DateTime.Today;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value ?? DateTime.Today;
                    OnPropertyChanged();
                    _ = LoadDataAsync();
                }
            }
        }

        // 显示模式：折线图 / 表格
        private string _displayMode = "折线图";
        public string DisplayMode
        {
            get => _displayMode;
            set
            {
                if (_displayMode != value)
                {
                    _displayMode = value;
                    OnPropertyChanged();
                    IsChartVisible = _displayMode == "折线图";
                    IsTableVisible = _displayMode == "表格";
                }
            }
        }

        private bool _isChartVisible = true;
        public bool IsChartVisible
        {
            get => _isChartVisible;
            set { _isChartVisible = value; OnPropertyChanged(); }
        }

        private bool _isTableVisible = false;
        public bool IsTableVisible
        {
            get => _isTableVisible;
            set { _isTableVisible = value; OnPropertyChanged(); }
        }

        private ObservableCollection<VitalSignsRecord> _tableData = new();
        public ObservableCollection<VitalSignsRecord> TableData
        {
            get => _tableData;
            set { _tableData = value; OnPropertyChanged(); }
        }

        public DrawMarginFrame DrawMarginFrame { get; set; } = new DrawMarginFrame
        {
            Fill = new SolidColorPaint(new SKColor(43, 43, 43)),
            Stroke = null
        };

        public SolidColorPaint LegendTextPaint { get; set; } = new SolidColorPaint(SKColors.White);

        public ICommand ReturnBedIndexCommand { get; }
        public ICommand RefreshCommand { get; }

        public _24hourDateViewModel(IServiceDataService serviceDataService, BedViewModel bed, Action onReturnToBedIndex)
        {
            _serviceDataService = serviceDataService;
            _bed = bed;
            _onReturnToBedIndex = onReturnToBedIndex;

            ReturnBedIndexCommand = new RelayCommand(ReturnToBedIndex);
            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());

            DateTime today = DateTime.Today;
            XAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("HH:mm"),
                    MinLimit = today.Ticks,
                    MaxLimit = today.AddDays(1).Ticks - 1,
                    MinStep = TimeSpan.FromHours(1).Ticks,
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(85, 85, 85))
                }
            };
            YAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(85, 85, 85))
                }
            };

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_bed == null || SelectedDate == null) return;

            List<VitalSignsRecord> records = null;
            try
            {
                records = await _serviceDataService.GetDataAsync(
                    _bed.DeviceIp, SelectedParameter, SelectedDate.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据加载失败: {ex.Message}");
                return;
            }

            DateTime queryDate = SelectedDate.Value.Date;
            XAxes[0].MinLimit = queryDate.Ticks;
            XAxes[0].MaxLimit = queryDate.AddDays(1).Ticks - 1;
            OnPropertyChanged(nameof(XAxes));

            TableData = new ObservableCollection<VitalSignsRecord>(records ?? new List<VitalSignsRecord>());

            if (records == null || records.Count == 0)
            {
                var emptySeries = CreateEmptyLineSeries(queryDate, "无数据", SKColors.Gray);
                Series = new ObservableCollection<ISeries> { emptySeries };
                OnPropertyChanged(nameof(Series));
                return;
            }

            var newSeries = new ObservableCollection<ISeries>();

            switch (SelectedParameter)
            {
                case "HR":
                    newSeries.Add(CreateLineSeries(records, r => r.HR ?? 0,
                        Application.Current.Resources["SeriesName_HR"]?.ToString() ?? "HR",
                        SKColors.Lime, queryDate));
                    UpdateYAxis(0, 200, Application.Current.Resources["Unit_bpm"]?.ToString() ?? "bpm");
                    break;
                case "SPO2":
                    newSeries.Add(CreateLineSeries(records, r => r.SPO2 ?? 0,
                        Application.Current.Resources["SeriesName_SpO2"]?.ToString() ?? "SpO2",
                        SKColors.Cyan, queryDate));
                    UpdateYAxis(0, 100, Application.Current.Resources["Unit_percent"]?.ToString() ?? "%");
                    break;
                case "RESP":
                    newSeries.Add(CreateLineSeries(records, r => r.RESP ?? 0,
                        Application.Current.Resources["SeriesName_RESP"]?.ToString() ?? "RESP",
                        SKColors.Yellow, queryDate));
                    UpdateYAxis(0, 60, Application.Current.Resources["Unit_rpm"]?.ToString() ?? "rpm");
                    break;
                case "NIBP":
                    newSeries.Add(CreateLineSeries(records, r => r.NIBP_SYS ?? 0,
                        Application.Current.Resources["SeriesName_NIBP_SYS"]?.ToString() ?? "SYS",
                        SKColors.Red, queryDate));
                    newSeries.Add(CreateLineSeries(records, r => r.NIBP_DIA ?? 0,
                        Application.Current.Resources["SeriesName_NIBP_DIA"]?.ToString() ?? "DIA",
                        SKColors.Orange, queryDate));
                    UpdateYAxis(0, 200, Application.Current.Resources["Unit_mmHg"]?.ToString() ?? "mmHg");
                    break;
                case "TEMP":
                    newSeries.Add(CreateLineSeries(records, r => r.TEMP ?? 0,
                        Application.Current.Resources["SeriesName_TEMP1"]?.ToString() ?? "TEMP1",
                        SKColors.White, queryDate));
                    newSeries.Add(CreateLineSeries(records, r => r.TEMP2 ?? 0,
                        Application.Current.Resources["SeriesName_TEMP2"]?.ToString() ?? "TEMP2",
                        SKColors.Orange, queryDate));
                    UpdateYAxis(30, 40, Application.Current.Resources["Unit_celsius"]?.ToString() ?? "℃");
                    break;
                case "PI":
                    newSeries.Add(CreateLineSeries(records, r => r.PI ?? 0,
                        Application.Current.Resources["SeriesName_PI"]?.ToString() ?? "PI",
                        SKColors.HotPink, queryDate));
                    UpdateYAxis(0, 10, Application.Current.Resources["Unit_percent"]?.ToString() ?? "%");
                    break;
            }

            Series = newSeries;
            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(YAxes));
        }

        private LineSeries<DateTimePoint> CreateLineSeries(
            List<VitalSignsRecord> records,
            Func<VitalSignsRecord, double> selector,
            string name,
            SKColor color,
            DateTime queryDate)
        {
            var fullDayPoints = new List<DateTimePoint>();
            for (int hour = 0; hour < 24; hour++)
            {
                fullDayPoints.Add(new DateTimePoint { DateTime = queryDate.AddHours(hour), Value = null });
            }

            foreach (var record in records)
            {
                int hour = record.RecordTime.Hour;
                if (hour >= 0 && hour < 24)
                {
                    fullDayPoints[hour].Value = selector(record);
                }
            }

            return new LineSeries<DateTimePoint>
            {
                Name = name,
                Values = fullDayPoints,
                Mapping = (point, index) => new(point.DateTime.Ticks, point.Value ?? double.NaN),
                Stroke = new SolidColorPaint(color, 2),
                Fill = new LinearGradientPaint(
                    new SKColor(color.Red, color.Green, color.Blue, 80),
                    new SKColor(color.Red, color.Green, color.Blue, 0)),
                GeometrySize = 10,
                GeometryFill = new SolidColorPaint(color),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2.5f },
                LineSmoothness = 0.3
            };
        }

        private LineSeries<DateTimePoint> CreateEmptyLineSeries(DateTime queryDate, string name, SKColor color)
        {
            var emptyPoints = new List<DateTimePoint>();
            for (int hour = 0; hour < 24; hour++)
                emptyPoints.Add(new DateTimePoint { DateTime = queryDate.AddHours(hour), Value = null });

            return new LineSeries<DateTimePoint>
            {
                Name = name,
                Values = emptyPoints,
                Mapping = (point, index) => new(point.DateTime.Ticks, double.NaN),
                Stroke = new SolidColorPaint(color) { StrokeThickness = 1 },
                GeometrySize = 0
            };
        }

        private void UpdateYAxis(double min, double max, string unit)
        {
            YAxes[0].MinLimit = min;
            YAxes[0].MaxLimit = max;
            YAxes[0].Name = unit;
            YAxes[0].Labeler = value => value.ToString("F0") + " " + unit;
            YAxes[0].NamePaint = new SolidColorPaint(SKColors.White);
        }

        private void ReturnToBedIndex() => _onReturnToBedIndex?.Invoke();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}