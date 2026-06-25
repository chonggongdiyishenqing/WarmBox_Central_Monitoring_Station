using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;
using WarmBox_Central_Monitoring_Station.ViewModel;
using System.Diagnostics;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Printing;

namespace WarmBox_Central_Monitoring_Station.Model
{
    public class Helper
    {
        public static class EncryptionHelper
        {
            // 使用DPAPI进行加密
            public static string Encrypt(string plainText)
            {
                if (string.IsNullOrEmpty(plainText))
                    return plainText;

                try
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] encryptedBytes = ProtectedData.Protect(
                        plainBytes,
                        null,
                        DataProtectionScope.CurrentUser);
                    return Convert.ToBase64String(encryptedBytes);
                }
                catch
                {
                    return plainText; // 加密失败时返回原文本
                }
            }

            // 使用DPAPI进行解密
            public static string Decrypt(string encryptedText)
            {
                if (string.IsNullOrEmpty(encryptedText))
                    return encryptedText;

                try
                {
                    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                    byte[] decryptedBytes = ProtectedData.Unprotect(
                        encryptedBytes,
                        null,
                        DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
                catch
                {
                    return encryptedText; // 解密失败时返回原文本
                }
            }
        }



      
    }


    public class VitalSignsRecord
    {
        public int ID { get; set; }
        public string DeviceIP { get; set; }
        public DateTime RecordTime { get; set; }
        public int? HR { get; set; }
        public int? NIBP_SYS { get; set; }
        public int? NIBP_DIA { get; set; }
        public int? SPO2 { get; set; }
        public int? RESP { get; set; }
        public double? TEMP { get; set; }   // 改为 double 以支持小数
        public double? TEMP2 { get; set; }  // 新增
        public int? PI { get; set; }
    }
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; }

        // 成功响应的静态工厂方法
        public static ApiResponse<T> SuccessFunction(T data, string message = "操作成功")
        {
            return new ApiResponse<T>
            {
                Success = true,
                StatusCode = 200,
                Message = message,
                Data = data
            };
        }

        // 失败响应的静态工厂方法
        public static ApiResponse<T> Failure(string message, int statusCode = 500)
        {
            return new ApiResponse<T>
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Data = default
            };
        }

        // 重载构造函数
        public ApiResponse(bool success, int statusCode, string message, T data = default)
        {
            Success = success;
            StatusCode = statusCode;
            Message = message;
            Data = data;
        }

        public ApiResponse() { }
    }

    public class PatientSyncInfo
    {
        public int? PatientId { get; set; }      // 病人ID（如果已存在则更新，否则新增）
        public string Name { get; set; }
        public string Gender { get; set; }        // "男" 或 "女"
        public DateTime? BirthDate { get; set; }
        public string GestationalAge { get; set; }
        public decimal? Weight { get; set; }      // 体重（克）
        public decimal? Height { get; set; }      // 身高（厘米）
        public int? AgeDays { get; set; }         // 日龄（天）
        public string BloodType { get; set; }
        public string DeviceNum { get; set; }     // 设备编号，用于关联设备与患者
    }
    public class AlarmLogItem
    {
        public string BedNumber { get; set; }
        public string PatientName { get; set; }
        public string AlarmType { get; set; }      // "设备报警"/"生理报警"
        public string AlarmContent { get; set; }   // 报警具体名称
        public string AlarmLevel { get; set; }     // "紧急" "警告" "轻微"
        public DateTime AlarmTime { get; set; }
        public string DeviceModel { get; set; }
        public string ParameterName { get; set; }
        public double? CurrentValue { get; set; }
    }
    public static class PasswordBoxHelper
        {
            public static readonly DependencyProperty BoundPasswordProperty =
                DependencyProperty.RegisterAttached("BoundPassword",
                    typeof(string),
                    typeof(PasswordBoxHelper),
                    new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

            public static readonly DependencyProperty BindPasswordProperty =
                DependencyProperty.RegisterAttached("BindPassword",
                    typeof(bool),
                    typeof(PasswordBoxHelper),
                    new PropertyMetadata(false, OnBindPasswordChanged));

            private static readonly DependencyProperty UpdatingPasswordProperty =
                DependencyProperty.RegisterAttached("UpdatingPassword",
                    typeof(bool),
                    typeof(PasswordBoxHelper),
                    new PropertyMetadata(false));

            public static void SetBoundPassword(DependencyObject dp, string value)
            {
                dp.SetValue(BoundPasswordProperty, value);
            }

            public static string GetBoundPassword(DependencyObject dp)
            {
                return (string)dp.GetValue(BoundPasswordProperty);
            }

            public static void SetBindPassword(DependencyObject dp, bool value)
            {
                dp.SetValue(BindPasswordProperty, value);
            }

            public static bool GetBindPassword(DependencyObject dp)
            {
                return (bool)dp.GetValue(BindPasswordProperty);
            }

            private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                PasswordBox box = d as PasswordBox;

                // 只处理来自绑定源的变化，避免递归
                if (d == null || !GetBindPassword(d))
                    return;

                box.PasswordChanged -= HandlePasswordChanged;

                string newPassword = (string)e.NewValue;

                if (!GetUpdatingPassword(d))
                {
                    box.Password = newPassword;
                }

                box.PasswordChanged += HandlePasswordChanged;
            }

            private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                PasswordBox box = d as PasswordBox;

                if (box == null)
                    return;

                bool wasBound = (bool)e.OldValue;
                bool needToBind = (bool)e.NewValue;

                if (wasBound)
                {
                    box.PasswordChanged -= HandlePasswordChanged;
                }

                if (needToBind)
                {
                    box.PasswordChanged += HandlePasswordChanged;
                }
            }

            private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
            {
                PasswordBox box = sender as PasswordBox;

                SetUpdatingPassword(box, true);
                SetBoundPassword(box, box.Password);
                SetUpdatingPassword(box, false);
            }

            private static bool GetUpdatingPassword(DependencyObject dp)
            {
                return (bool)dp.GetValue(UpdatingPasswordProperty);
            }

            private static void SetUpdatingPassword(DependencyObject dp, bool value)
            {
                dp.SetValue(UpdatingPasswordProperty, value);
            }
        }

    public static class BedConfigEventPublisher
    {
        public static event Action BedConfigChanged;

        public static void NotifyBedConfigChanged()
        {
            BedConfigChanged?.Invoke();
        }
    }

    //public class SimpleWaveformRenderer : IDisposable
    //{
    //    private readonly Canvas _canvas;
    //    private readonly Brush _stroke;
    //    private readonly double _strokeThickness;
    //    private readonly double _pointSpacing;
    //    private readonly int _maxPoints;          // 实际保留的最大点数（基于画布宽度自动计算）
    //    private readonly bool _fixedYRange;
    //    private readonly double _fixedMinY;
    //    private readonly double _fixedMaxY;
    //    private readonly int _batchSize;          // 每批取点数
    //    private readonly int _intervalMs;          // 定时器间隔（毫秒）

    //    private Polyline _polyline;
    //    private PointCollection _points;
    //    private DispatcherTimer _timer;
    //    private Queue<double> _dataQueue = new Queue<double>();
    //    private readonly object _queueLock = new object();
    //    private int _totalPointsDrawn = 0;        // 累计绘制的总点数（用于X坐标）
    //    private double _canvasHeight;
    //    private double _fixedCanvasWidth;          // 画布的固定宽度
    //    private double _dynamicMinY = double.MaxValue;
    //    private double _dynamicMaxY = double.MinValue;
    //    private bool _addInitialPoints; // 新增字段

    //    public bool IsRunning { get; private set; }

    //    /// <summary>
    //    /// 构造函数
    //    /// </summary>
    //    /// <param name="canvas">要绘制的Canvas（必须已设置固定宽度）</param>
    //    /// <param name="stroke">线条颜色</param>
    //    /// <param name="strokeThickness">线条粗细</param>
    //    /// <param name="pointSpacing">点间距（像素）</param>
    //    /// <param name="batchSize">每批取点数</param>
    //    /// <param name="intervalMs">定时器间隔（毫秒）</param>
    //    /// <param name="fixedMinY">固定Y轴最小值（若为null则动态范围）</param>
    //    /// <param name="fixedMaxY">固定Y轴最大值</param>
    //    /// <param name="addInitialPoints">是否添加初始占位点</param>
    //    public SimpleWaveformRenderer(Canvas canvas, Brush stroke, double strokeThickness = 1.0,
    //                                  double pointSpacing = 1.0, int batchSize = 3, int intervalMs = 10,
    //                                  double? fixedMinY = null, double? fixedMaxY = null,
    //                                  bool addInitialPoints = true)
    //    {
    //        _canvas = canvas;
    //        _stroke = stroke;
    //        _strokeThickness = strokeThickness;
    //        _pointSpacing = pointSpacing;
    //        _batchSize = batchSize;
    //        _intervalMs = intervalMs;
    //        _fixedYRange = fixedMinY.HasValue && fixedMaxY.HasValue;
    //        _fixedMinY = fixedMinY ?? 0;
    //        _fixedMaxY = fixedMaxY ?? 1;
    //        _addInitialPoints = addInitialPoints;
    //        Initialize();
    //    }

    //    private void Initialize()
    //    {
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            _canvas.Children.Clear();

    //            _polyline = new Polyline
    //            {
    //                Stroke = _stroke,
    //                StrokeThickness = _strokeThickness,
    //                StrokeLineJoin = PenLineJoin.Round
    //            };

    //            _points = new PointCollection();
    //            _polyline.Points = _points;
    //            _canvas.Children.Add(_polyline);

    //            // 获取画布固定宽度
    //            _fixedCanvasWidth = _canvas.Width > 0 ? _canvas.Width : 500;
    //            _canvas.Width = _fixedCanvasWidth; // 确保固定

    //            // 获取画布高度，留出边距
    //            _canvasHeight = _canvas.ActualHeight > 0 ? _canvas.ActualHeight :
    //                            (_canvas.Height > 0 ? _canvas.Height : 80);
    //            if (_canvasHeight <= 0) _canvasHeight = 80;
    //            // 留出上下边距（10像素）
    //            _canvasHeight -= 10;
    //            if (_canvasHeight < 20) _canvasHeight = 20;

    //            if (_addInitialPoints)
    //            {
    //                for (int i = 0; i < 5; i++)
    //                {
    //                    double x = i * _pointSpacing;
    //                    double y = _canvasHeight / 2;
    //                    _points.Add(new Point(x, y));
    //                }
    //                _totalPointsDrawn = 5;
    //            }
    //            else
    //            {
    //                _totalPointsDrawn = 0; // 无初始点
    //            }
    //        });

    //        _timer = new DispatcherTimer
    //        {
    //            Interval = TimeSpan.FromMilliseconds(_intervalMs)
    //        };
    //        _timer.Tick += Timer_Tick;
    //    }

    //    private void Timer_Tick(object sender, EventArgs e)
    //    {
    //        List<double> newValues = new List<double>();
    //        lock (_queueLock)
    //        {
    //            int count = Math.Min(_batchSize, _dataQueue.Count);
    //            for (int i = 0; i < count; i++)
    //            {
    //                newValues.Add(_dataQueue.Dequeue());
    //            }
    //        }

    //        if (newValues.Count == 0) return;

    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            foreach (double val in newValues)
    //            {
    //                double x = _totalPointsDrawn * _pointSpacing;
    //                double y = MapValueToY(val);
    //                _points.Add(new Point(x, y));
    //                _totalPointsDrawn++;
    //            }

    //            // 检查是否超出固定宽度
    //            double totalWidth = _totalPointsDrawn * _pointSpacing;
    //            if (totalWidth > _fixedCanvasWidth)
    //            {
    //                // 需要保留的点数（填满画布）
    //                int pointsToKeep = (int)(_fixedCanvasWidth / _pointSpacing);
    //                if (pointsToKeep < 1) pointsToKeep = 1;
    //                int removeCount = _points.Count - pointsToKeep;
    //                if (removeCount > 0)
    //                {
    //                    var newPoints = new PointCollection();
    //                    for (int i = removeCount; i < _points.Count; i++)
    //                    {
    //                        var p = _points[i];
    //                        newPoints.Add(new Point(p.X - removeCount * _pointSpacing, p.Y));
    //                    }
    //                    _points = newPoints;
    //                    _polyline.Points = _points;
    //                    _totalPointsDrawn -= removeCount;
    //                }
    //            }
    //        });
    //    }

    //    private double MapValueToY(double value)
    //    {
    //        double minY, maxY;
    //        if (_fixedYRange)
    //        {
    //            minY = _fixedMinY;
    //            maxY = _fixedMaxY;
    //        }
    //        else
    //        {
    //            minY = _dynamicMinY;
    //            maxY = _dynamicMaxY;
    //            if (maxY - minY < 0.1) maxY = minY + 1; // 避免除零
    //        }

    //        double normalized = (value - minY) / (maxY - minY);
    //        normalized = Math.Max(0, Math.Min(1, normalized)); // 钳位
    //                                                           // 映射到画布高度，并留出边距
    //        double margin = 5;
    //        double y = margin + (1 - normalized) * (_canvasHeight - 2 * margin);
    //        return y;
    //    }

    //    /// <summary>
    //    /// 添加单个数据点
    //    /// </summary>
    //    public void AddDataPoint(double value)
    //    {
    //        lock (_queueLock)
    //        {
    //            _dataQueue.Enqueue(value);
    //            if (!_fixedYRange)
    //            {
    //                if (value < _dynamicMinY) _dynamicMinY = value;
    //                if (value > _dynamicMaxY) _dynamicMaxY = value;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 批量添加数据点
    //    /// </summary>
    //    public void AddDataPoints(IEnumerable<double> values)
    //    {
    //        lock (_queueLock)
    //        {
    //            foreach (var val in values)
    //            {
    //                _dataQueue.Enqueue(val);
    //                if (!_fixedYRange)
    //                {
    //                    if (val < _dynamicMinY) _dynamicMinY = val;
    //                    if (val > _dynamicMaxY) _dynamicMaxY = val;
    //                }
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 添加CSV格式数据（逗号分隔）
    //    /// </summary>
    //    public void AddCsvData(string csvData)
    //    {
    //        if (string.IsNullOrEmpty(csvData)) return;

    //        var values = new List<double>();
    //        var parts = csvData.Split(new[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
    //        foreach (var part in parts)
    //        {
    //            if (double.TryParse(part, out double val))
    //            {
    //                values.Add(val);
    //            }
    //        }
    //        if (values.Count > 0)
    //        {
    //            AddDataPoints(values);
    //        }
    //    }

    //    public void Start()
    //    {
    //        if (_timer != null && !_timer.IsEnabled)
    //        {
    //            _timer.Start();
    //            IsRunning = true;
    //        }
    //    }

    //    public void Stop()
    //    {
    //        if (_timer != null && _timer.IsEnabled)
    //        {
    //            _timer.Stop();
    //            IsRunning = false;
    //        }
    //    }

    //    public void Clear()
    //    {
    //        lock (_queueLock)
    //        {
    //            _dataQueue.Clear();
    //        }
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            _points.Clear();
    //            _totalPointsDrawn = 0;
    //            _dynamicMinY = double.MaxValue;
    //            _dynamicMaxY = double.MinValue;
    //        });
    //    }

    //    public void Dispose()
    //    {
    //        Stop();
    //        _timer = null;
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            if (_canvas != null)
    //            {
    //                _canvas.Children.Clear();
    //            }
    //        });
    //    }
    //}
    //public class SimpleWaveformRenderer : IDisposable
    //{
    //    private readonly Canvas _canvas;
    //    private readonly Brush _stroke;
    //    private readonly double _strokeThickness;
    //    private readonly double _pointSpacing;
    //    private readonly bool _fixedYRange;
    //    private readonly double _fixedMinY;
    //    private readonly double _fixedMaxY;
    //    private readonly int _batchSize;
    //    private readonly int _intervalMs;
    //    private readonly bool _addInitialPoints;

    //    private Polyline _polyline;
    //    private PointCollection _points;
    //    private DispatcherTimer _timer;
    //    private Queue<double> _dataQueue = new Queue<double>();
    //    private readonly object _queueLock = new object();
    //    private int _totalPointsDrawn = 0;
    //    private double _canvasHeight;
    //    private double _fixedCanvasWidth;      // 当前画布固定宽度（动态更新）
    //    private double _dynamicMinY = double.MaxValue;
    //    private double _dynamicMaxY = double.MinValue;

    //    public bool IsRunning { get; private set; }

    //    public SimpleWaveformRenderer(Canvas canvas, Brush stroke, double strokeThickness = 1.0,
    //                                  double pointSpacing = 1.0, int batchSize = 3, int intervalMs = 10,
    //                                  double? fixedMinY = null, double? fixedMaxY = null,
    //                                  bool addInitialPoints = true)
    //    {
    //        _canvas = canvas;
    //        _stroke = stroke;
    //        _strokeThickness = strokeThickness;
    //        _pointSpacing = pointSpacing;
    //        _batchSize = batchSize;
    //        _intervalMs = intervalMs;
    //        _fixedYRange = fixedMinY.HasValue && fixedMaxY.HasValue;
    //        _fixedMinY = fixedMinY ?? 0;
    //        _fixedMaxY = fixedMaxY ?? 1;
    //        _addInitialPoints = addInitialPoints;

    //        Initialize();
    //    }

    //    private void Initialize()
    //    {
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            _canvas.Children.Clear();

    //            _polyline = new Polyline
    //            {
    //                Stroke = _stroke,
    //                StrokeThickness = _strokeThickness,
    //                StrokeLineJoin = PenLineJoin.Round
    //            };

    //            _points = new PointCollection();
    //            _polyline.Points = _points;
    //            _canvas.Children.Add(_polyline);

    //            // 初始获取画布实际宽度（可能为0，稍后在第一次更新时再处理）
    //            UpdateCanvasSize();

    //            // 获取画布高度（留边距）
    //            _canvasHeight = _canvas.ActualHeight > 0 ? _canvas.ActualHeight :
    //                            (_canvas.Height > 0 ? _canvas.Height : 80);
    //            if (_canvasHeight <= 0) _canvasHeight = 80;
    //            _canvasHeight -= 10; // 上下边距
    //            if (_canvasHeight < 20) _canvasHeight = 20;

    //            if (_addInitialPoints && _fixedCanvasWidth > 0)
    //            {
    //                int initialCount = (int)(_fixedCanvasWidth / _pointSpacing / 2);
    //                initialCount = Math.Min(initialCount, 20);
    //                for (int i = 0; i < initialCount; i++)
    //                {
    //                    double x = i * _pointSpacing;
    //                    double y = _canvasHeight / 2;
    //                    _points.Add(new Point(x, y));
    //                }
    //                _totalPointsDrawn = initialCount;
    //            }
    //            else
    //            {
    //                _totalPointsDrawn = 0;
    //            }
    //        });

    //        _timer = new DispatcherTimer
    //        {
    //            Interval = TimeSpan.FromMilliseconds(_intervalMs)
    //        };
    //        _timer.Tick += Timer_Tick;
    //    }

    //    /// <summary>
    //    /// 更新画布尺寸，当父容器大小改变时调用
    //    /// </summary>
    //    public void UpdateCanvasSize()
    //    {
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            if (_canvas == null) return;

    //            double newWidth = _canvas.ActualWidth;
    //            if (newWidth <= 0) newWidth = _canvas.Width;
    //            if (newWidth <= 0) newWidth = 500;

    //            if (Math.Abs(newWidth - _fixedCanvasWidth) < 0.1) return;

    //            _fixedCanvasWidth = newWidth;
    //            _canvas.Width = _fixedCanvasWidth;

    //            // 重新调整当前点集，使波形填满新宽度
    //            if (_points == null || _points.Count == 0) return;

    //            int pointsToKeep = (int)(_fixedCanvasWidth / _pointSpacing);
    //            if (pointsToKeep < 1) pointsToKeep = 1;

    //            if (_points.Count > pointsToKeep)
    //            {
    //                int removeCount = _points.Count - pointsToKeep;
    //                var newPoints = new PointCollection();
    //                for (int i = removeCount; i < _points.Count; i++)
    //                {
    //                    var p = _points[i];
    //                    newPoints.Add(new Point(p.X - removeCount * _pointSpacing, p.Y));
    //                }
    //                _points = newPoints;
    //                _polyline.Points = _points;
    //                _totalPointsDrawn -= removeCount;
    //            }
    //            else if (_points.Count < pointsToKeep && _totalPointsDrawn == _points.Count)
    //            {
    //                // 如果当前点数不足，可以补充空白？不处理，等待新数据自然填充
    //            }
    //        });
    //    }

    //    private void Timer_Tick(object sender, EventArgs e)
    //    {
    //        List<double> newValues = new List<double>();
    //        lock (_queueLock)
    //        {
    //            int count = Math.Min(_batchSize, _dataQueue.Count);
    //            for (int i = 0; i < count; i++)
    //            {
    //                newValues.Add(_dataQueue.Dequeue());
    //            }
    //        }

    //        if (newValues.Count == 0) return;

    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            if (_fixedCanvasWidth <= 0) UpdateCanvasSize();

    //            foreach (double val in newValues)
    //            {
    //                double x = _totalPointsDrawn * _pointSpacing;
    //                double y = MapValueToY(val);
    //                _points.Add(new Point(x, y));
    //                _totalPointsDrawn++;
    //            }

    //            double totalWidth = _totalPointsDrawn * _pointSpacing;
    //            if (totalWidth > _fixedCanvasWidth)
    //            {
    //                int pointsToKeep = (int)(_fixedCanvasWidth / _pointSpacing);
    //                if (pointsToKeep < 1) pointsToKeep = 1;
    //                int removeCount = _points.Count - pointsToKeep;
    //                if (removeCount > 0)
    //                {
    //                    var newPoints = new PointCollection();
    //                    for (int i = removeCount; i < _points.Count; i++)
    //                    {
    //                        var p = _points[i];
    //                        newPoints.Add(new Point(p.X - removeCount * _pointSpacing, p.Y));
    //                    }
    //                    _points = newPoints;
    //                    _polyline.Points = _points;
    //                    _totalPointsDrawn -= removeCount;
    //                }
    //            }
    //        });
    //    }

    //    private double MapValueToY(double value)
    //    {
    //        double minY, maxY;
    //        if (_fixedYRange)
    //        {
    //            minY = _fixedMinY;
    //            maxY = _fixedMaxY;
    //        }
    //        else
    //        {
    //            minY = _dynamicMinY;
    //            maxY = _dynamicMaxY;
    //            if (maxY - minY < 0.1) maxY = minY + 1;
    //        }

    //        double normalized = (value - minY) / (maxY - minY);
    //        normalized = Math.Max(0, Math.Min(1, normalized));
    //        double margin = 5;
    //        double y = margin + (1 - normalized) * (_canvasHeight - 2 * margin);
    //        return y;
    //    }

    //    public void AddDataPoint(double value)
    //    {
    //        lock (_queueLock)
    //        {
    //            _dataQueue.Enqueue(value);
    //            if (!_fixedYRange)
    //            {
    //                if (value < _dynamicMinY) _dynamicMinY = value;
    //                if (value > _dynamicMaxY) _dynamicMaxY = value;
    //            }
    //        }
    //    }

    //    public void AddDataPoints(IEnumerable<double> values)
    //    {
    //        lock (_queueLock)
    //        {
    //            foreach (var val in values)
    //            {
    //                _dataQueue.Enqueue(val);
    //                if (!_fixedYRange)
    //                {
    //                    if (val < _dynamicMinY) _dynamicMinY = val;
    //                    if (val > _dynamicMaxY) _dynamicMaxY = val;
    //                }
    //            }
    //        }
    //    }

    //    public void AddCsvData(string csvData)
    //    {
    //        if (string.IsNullOrEmpty(csvData)) return;

    //        var values = new List<double>();
    //        var parts = csvData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    //        foreach (var part in parts)
    //        {
    //            if (double.TryParse(part, out double val))
    //            {
    //                values.Add(val);
    //            }
    //        }
    //        if (values.Count > 0)
    //        {
    //            AddDataPoints(values);
    //        }
    //    }

    //    public void Start()
    //    {
    //        if (_timer != null && !_timer.IsEnabled)
    //        {
    //            _timer.Start();
    //            IsRunning = true;
    //        }
    //    }

    //    public void Stop()
    //    {
    //        if (_timer != null && _timer.IsEnabled)
    //        {
    //            _timer.Stop();
    //            IsRunning = false;
    //        }
    //    }

    //    public void Clear()
    //    {
    //        lock (_queueLock)
    //        {
    //            _dataQueue.Clear();
    //        }
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            _points.Clear();
    //            _totalPointsDrawn = 0;
    //            _dynamicMinY = double.MaxValue;
    //            _dynamicMaxY = double.MinValue;
    //        });
    //    }

    //    public void Dispose()
    //    {
    //        Stop();
    //        _timer = null;
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            if (_canvas != null)
    //            {
    //                _canvas.Children.Clear();
    //            }
    //        });
    //    }
    //}
    public class SimpleWaveformRenderer : IDisposable
    {
        private readonly Canvas _canvas;
        private readonly Brush _stroke;
        private readonly double _strokeThickness;
        private readonly double _pointSpacing;
        private readonly bool _fixedYRange;
        private readonly double _fixedMinY;
        private readonly double _fixedMaxY;
        private readonly int _batchSize;
        private readonly int _intervalMs;
        private readonly bool _addInitialPoints;

        private Polyline _polyline;
        private PointCollection _points;
        private DispatcherTimer _timer;
        private Queue<double> _dataQueue = new();
        private readonly object _queueLock = new();
        private int _totalPointsDrawn = 0;
        private double _canvasHeight;
        private double _fixedCanvasWidth;
        private double _dynamicMinY = double.MaxValue;
        private double _dynamicMaxY = double.MinValue;

        // 用于调试丢点统计
        private int _parseFailCount = 0;

        public bool IsRunning { get; private set; }

        public SimpleWaveformRenderer(Canvas canvas, Brush stroke, double strokeThickness = 1.0,
                                      double pointSpacing = 1.0, int batchSize = 3, int intervalMs = 10,
                                      double? fixedMinY = null, double? fixedMaxY = null,
                                      bool addInitialPoints = true)
        {
            _canvas = canvas;
            _stroke = stroke;
            _strokeThickness = strokeThickness;
            _pointSpacing = pointSpacing;
            _batchSize = batchSize;
            _intervalMs = intervalMs;
            _fixedYRange = fixedMinY.HasValue && fixedMaxY.HasValue;
            _fixedMinY = fixedMinY ?? 0;
            _fixedMaxY = fixedMaxY ?? 1;
            _addInitialPoints = addInitialPoints;

            Initialize();
        }

        private void Initialize()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _canvas.Children.Clear();
                _polyline = new Polyline
                {
                    Stroke = _stroke,
                    StrokeThickness = _strokeThickness,
                    StrokeLineJoin = PenLineJoin.Round
                };
                _points = new PointCollection();
                _polyline.Points = _points;
                _canvas.Children.Add(_polyline);

                UpdateCanvasSize();

                _canvasHeight = _canvas.ActualHeight > 0 ? _canvas.ActualHeight :
                                (_canvas.Height > 0 ? _canvas.Height : 80);
                if (_canvasHeight <= 0) _canvasHeight = 80;
                _canvasHeight -= 10; // 上下边距
                if (_canvasHeight < 20) _canvasHeight = 20;

                if (_addInitialPoints && _fixedCanvasWidth > 0)
                {
                    int initialCount = Math.Min((int)(_fixedCanvasWidth / _pointSpacing / 2), 20);
                    for (int i = 0; i < initialCount; i++)
                    {
                        _points.Add(new Point(i * _pointSpacing, _canvasHeight / 2));
                    }
                    _totalPointsDrawn = initialCount;
                }
                else
                {
                    _totalPointsDrawn = 0;
                }
            });

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_intervalMs) };
            _timer.Tick += Timer_Tick;
        }

        public void UpdateCanvasSize()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_canvas == null) return;
                double newWidth = _canvas.ActualWidth;
                if (newWidth <= 0) newWidth = _canvas.Width;
                if (newWidth <= 0) newWidth = 500;

                if (Math.Abs(newWidth - _fixedCanvasWidth) < 0.1) return;
                _fixedCanvasWidth = newWidth;
                _canvas.Width = _fixedCanvasWidth;

                if (_points == null || _points.Count == 0) return;
                int pointsToKeep = Math.Max((int)(_fixedCanvasWidth / _pointSpacing), 1);
                if (_points.Count > pointsToKeep)
                {
                    int removeCount = _points.Count - pointsToKeep;
                    var newPoints = new PointCollection();
                    for (int i = removeCount; i < _points.Count; i++)
                    {
                        var p = _points[i];
                        newPoints.Add(new Point(p.X - removeCount * _pointSpacing, p.Y));
                    }
                    _points = newPoints;
                    _polyline.Points = _points;
                    _totalPointsDrawn -= removeCount;
                }
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            List<double> batch = new();
            lock (_queueLock)
            {
                int count = Math.Min(_batchSize, _dataQueue.Count);
                for (int i = 0; i < count; i++)
                    batch.Add(_dataQueue.Dequeue());
            }

            if (batch.Count == 0) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                // 确保尺寸有效
                if (_fixedCanvasWidth <= 0) UpdateCanvasSize();
                if (_fixedCanvasWidth <= 0 || _canvasHeight <= 0) return; // 尺寸无效时保留数据在队列中

                foreach (double val in batch)
                {
                    double x = _totalPointsDrawn * _pointSpacing;
                    double y = MapValueToY(val);
                    _points.Add(new Point(x, y));
                    _totalPointsDrawn++;
                }

                // 裁剪旧点
                double totalWidth = _totalPointsDrawn * _pointSpacing;
                if (totalWidth > _fixedCanvasWidth)
                {
                    int keep = Math.Max((int)(_fixedCanvasWidth / _pointSpacing), 1);
                    int removeCount = _points.Count - keep;
                    if (removeCount > 0)
                    {
                        var newPoints = new PointCollection();
                        for (int i = removeCount; i < _points.Count; i++)
                        {
                            var p = _points[i];
                            newPoints.Add(new Point(p.X - removeCount * _pointSpacing, p.Y));
                        }
                        _points = newPoints;
                        _polyline.Points = _points;
                        _totalPointsDrawn -= removeCount;
                    }
                }
            });
        }

        private double MapValueToY(double value)
        {
            double minY, maxY;
            if (_fixedYRange)
            {
                minY = _fixedMinY;
                maxY = _fixedMaxY;
            }
            else
            {
                minY = _dynamicMinY;
                maxY = _dynamicMaxY;
                if (maxY - minY < 0.1) maxY = minY + 1;
            }
            double normalized = (value - minY) / (maxY - minY);
            normalized = Math.Max(0, Math.Min(1, normalized));
            double margin = 5;
            double y = margin + (1 - normalized) * (_canvasHeight - 2 * margin);
            return y;
        }

        public void AddDataPoint(double value)
        {
            lock (_queueLock)
            {
                _dataQueue.Enqueue(value);
                if (!_fixedYRange)
                {
                    if (value < _dynamicMinY) _dynamicMinY = value;
                    if (value > _dynamicMaxY) _dynamicMaxY = value;
                }
            }
        }

        public void AddDataPoints(IEnumerable<double> values)
        {
            lock (_queueLock)
            {
                foreach (var val in values)
                {
                    _dataQueue.Enqueue(val);
                    if (!_fixedYRange)
                    {
                        if (val < _dynamicMinY) _dynamicMinY = val;
                        if (val > _dynamicMaxY) _dynamicMaxY = val;
                    }
                }
            }
        }

        public void AddCsvData(string csvData)
        {
            if (string.IsNullOrEmpty(csvData)) return;

            // 强力清洗：只保留数字、逗号、负号
            var cleanBuilder = new StringBuilder();
            foreach (char c in csvData)
            {
                if (char.IsDigit(c) || c == ',' || c == '-')
                    cleanBuilder.Append(c);
            }
            string cleaned = cleanBuilder.ToString();

            // 修复常见错误：将粘连的数字-数字拆开（如 "2-6" -> "2,-6"）
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"(\d)-(-?\d)",
                "$1,$2");

            // 分割并解析
            string[] parts = cleaned.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var values = new List<double>();
            foreach (string part in parts)
            {
                // 跳过孤立负号
                if (part == "-" || part == "--")
                    continue;

                if (double.TryParse(part, out double val))
                {
                    values.Add(val);
                }
                else
                {
                    // 依然无效，记录日志（可注释掉）
                    System.Diagnostics.Debug.WriteLine($"[AddCsvData] 无效数值: '{part}'，原始片段: '{csvData.Substring(0, Math.Min(csvData.Length, 80))}'");
                }
            }

            if (values.Count > 0)
                AddDataPoints(values);
        }

        public void Start()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_intervalMs) };
                _timer.Tick += Timer_Tick;
            }
            if (!_timer.IsEnabled)
            {
                _timer.Start();
                IsRunning = true;
            }
        }

        public void Stop()
        {
            if (_timer?.IsEnabled == true)
            {
                _timer.Stop();
                IsRunning = false;
            }
        }

        public void Clear()
        {
            lock (_queueLock) _dataQueue.Clear();
            Application.Current.Dispatcher.Invoke(() =>
            {
                _points?.Clear();
                _totalPointsDrawn = 0;
                _dynamicMinY = double.MaxValue;
                _dynamicMaxY = double.MinValue;
            });
        }

        public void Dispose()
        {
            Stop();
            _timer = null;
            Application.Current.Dispatcher.Invoke(() => _canvas?.Children.Clear());
        }
    }
    public static class ToastHelper
    {
        public static void Show(string message, string colorHex = "#FFFFFF", int durationMs = 2000)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Color textColor;
                try { textColor = (Color)ColorConverter.ConvertFromString(colorHex); }
                catch { textColor = Colors.White; }

                var textBlock = new TextBlock
                {
                    Text = message,
                    FontSize = 16,
                    Foreground = new SolidColorBrush(textColor),
                    Background = Brushes.Transparent,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(24, 12, 24, 12),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var border = new Border
                {
                    Child = textBlock,
                    Background = new SolidColorBrush(Color.FromArgb(200, 30, 30, 35)), // 半透明深色背景
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Opacity = 0,
                    RenderTransform = new TranslateTransform { Y = -20 }
                };

                var popup = new Popup
                {
                    AllowsTransparency = true,
                    PopupAnimation = PopupAnimation.Fade,
                    Placement = PlacementMode.Absolute,
                    HorizontalOffset = (SystemParameters.WorkArea.Width - 280) / 2,
                    VerticalOffset = 60,
                    Child = border
                };

                // 根据内容自动设置大小
                border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                popup.Width = border.DesiredSize.Width;
                popup.Height = border.DesiredSize.Height;
                popup.IsOpen = true;

                // 淡入动画
                var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(300));
                var slideIn = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new QuadraticEase()
                };
                border.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                (border.RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.YProperty, slideIn);

                // 自动关闭
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
                    fadeOut.Completed += (_, _) => popup.IsOpen = false;
                    border.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                };
                timer.Start();
            });
        }
    }

    public static class PrintHelper
    {
        /// <summary>
        /// 同步打印 UI 元素，内部检测打印机状态，防止脱机卡死
        /// </summary>
        public static bool PrintUIElement(FrameworkElement element, string jobDescription = "波形打印")
        {
            if (element == null) return false;

            // 1. 打开打印对话框
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
                return false;

            // 2. 检查所选打印机是否可用
            if (printDialog.PrintQueue == null)
            {
                MessageBox.Show("未找到可用打印机。", "打印错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            PrintQueue printer = printDialog.PrintQueue;
            if (printer.IsOffline)
            {
                MessageBox.Show($"打印机 {printer.FullName} 处于脱机状态，请检查连接。", "打印机脱机",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (printer.IsInError)
            {
                MessageBox.Show($"打印机 {printer.FullName} 出现错误，请检查。", "打印机错误",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            // 检查是否有缺纸的打印作业
            bool hasPaperOut = printer.GetPrintJobInfoCollection()
                .Any(job => job.JobStatus == PrintJobStatus.PaperOut);
            if (hasPaperOut)
            {
                MessageBox.Show($"打印机 {printer.FullName} 缺纸，请补充纸张。", "缺纸",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }


            // 3. 渲染要打印的内容
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Arrange(new Rect(element.DesiredSize));
            element.UpdateLayout();

            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)element.ActualWidth, (int)element.ActualHeight,
                96, 96, PixelFormats.Pbgra32);
            bitmap.Render(element);

            Image image = new Image
            {
                Source = bitmap,
                Stretch = Stretch.None
            };

            // 4. 执行打印（此时打印机已知可用，不会长时间阻塞）
            try
            {
                printDialog.PrintVisual(image, jobDescription);
                return true;
            }
            catch (Exception ex)
            {
                // 极少情况下打印过程中可能发生异常（如打印机突然断开）
                MessageBox.Show($"打印失败: {ex.Message}", "打印错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

}
