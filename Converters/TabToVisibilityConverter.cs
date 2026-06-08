using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using WarmBox_Central_Monitoring_Station.ViewModel;

namespace WarmBox_Central_Monitoring_Station.Converters    
    {
        public class TabToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value?.ToString() == parameter?.ToString())
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class BooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class TabToCommandConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length >= 3 && values[0] is string tabHeader)
                {
                    return tabHeader == "完全新增" ? values[1] : values[2];
                }
                return null;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class InverseBooleanConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is bool boolValue ? !boolValue : value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is bool boolValue ? !boolValue : value;
            }
        }

        // 添加缺失的 InverseBooleanToVisibilityConverter
        public class InverseBooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                return Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is Visibility visibility)
                {
                    return visibility != Visibility.Visible;
                }
                return false;
            }
        }

        public class StringToBrushConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is string colorString)
                {
                    return colorString.ToUpper() switch
                    {
                        "GREEN" => Brushes.Green,
                        "WHITE" => Brushes.White,
                        "#00FF00" => new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                        "#02DADA" => new SolidColorBrush(Color.FromRgb(2, 218, 218)),
                        "ORANGE" => Brushes.Orange,
                        _ => Brushes.White
                    };
                }
                return Brushes.White;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }


        public class BooleanToVisibilityConverter2 : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool isVisible = (bool)value;

                // 如果parameter为"inverse"，则反转逻辑
                if (parameter != null && parameter.ToString() == "inverse")
                {
                    isVisible = !isVisible;
                }

                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                Visibility visibility = (Visibility)value;
                return visibility == Visibility.Visible;
            }
        }

        public class IsStringNotEmptyConverter : IValueConverter
        {
            // 单例模式
            public static readonly IsStringNotEmptyConverter Instance = new IsStringNotEmptyConverter();

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return !string.IsNullOrEmpty(value as string);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class IntToBoolConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int selectedMode && parameter is string param && int.TryParse(param, out int mode))
                {
                    return selectedMode == mode;
                }
                return false;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool isChecked && isChecked && parameter is string param && int.TryParse(param, out int mode))
                {
                    return mode;
                }
                return Binding.DoNothing;
            }
        }

        public class ViewModeToPageCountConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int viewMode)
                {
                    return viewMode switch
                    {
                        8 => 4,   // 8视图：4个分页按钮
                        16 => 2,  // 16视图：2个分页按钮
                        32 => 0,  // 32视图：0个分页按钮
                        _ => 0
                    };
                }
                return 0;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }



        public class ViewModeToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int viewMode)
                {
                    // 32视图不显示分页按钮，其他都显示
                    return viewMode == 32 ? Visibility.Collapsed : Visibility.Visible;
                }
                return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        // 根据视图模式控制按钮显示
        public class ViewModeToButtonVisibilityConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length >= 2 && values[0] is int viewMode && values[1] is int buttonIndex)
                {
                    // 按钮索引从1开始
                    return viewMode switch
                    {
                        8 => Visibility.Visible,  // 8视图：所有按钮都显示
                        16 => buttonIndex <= 2 ? Visibility.Visible : Visibility.Collapsed, // 16视图：只显示1和2
                        _ => Visibility.Collapsed // 32视图或其他：不显示
                    };
                }
                return Visibility.Collapsed;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        // 添加一个IntToVisibilityConverter
        public class IntToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int viewMode && parameter is string paramStr)
                {
                    int param = int.Parse(paramStr);
                    return viewMode == param ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class InvertBoolConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    return !boolValue;
                }
                return false;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    return !boolValue;
                }
                return false;
            }
        }

        // 或者创建一个反转的可见性转换器
        public class InvertBoolToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                return Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }



    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }

    public class BoolToInvertVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && !b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Collapsed;
        }
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Brushes.White : Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBorderThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? new Thickness(1) : new Thickness(0.35);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProgressToWidthConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double progressValue)
                {
                    // 假设 Minimum=0, Maximum=100
                    // 返回百分比宽度（0-1之间的值）
                    double percentage = progressValue / 100.0;

                    // 或者如果绑定到有父元素宽度的控件，这里可以进一步处理
                    // 对于这个简单场景，我们直接返回百分比
                    return new GridLength(percentage, GridUnitType.Star);
                }
                return 0;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class ShowButtonConverter : IValueConverter
        {

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        public class SliderWidthConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double sliderValue)
                {
                    // 假设最大宽度为200px，对应Value=100
                    // 使用参数可以灵活调整，如果parameter有值则使用参数作为最大宽度
                    double maxWidth = 200;
                    if (parameter != null && double.TryParse(parameter.ToString(), out double paramWidth))
                    {
                        maxWidth = paramWidth;
                    }

                    // 将0-100的值转换为0-maxWidth的宽度
                    return sliderValue * (maxWidth / 100.0);
                }
                return 0;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class ProgressWidthConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length == 3 &&
                    values[0] is double value &&
                    values[1] is double maximum &&
                    values[2] is double trackWidth)
                {
                    if (maximum == 0) return 0.0;

                    // 计算百分比宽度
                    double percentage = value / maximum;
                    return trackWidth * percentage;
                }
                return 0.0;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class GreaterThanOneToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int count && count > 1)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        // 当值大于2时显示，否则隐藏
        public class GreaterThanTwoToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int count && count > 2)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class WidthToHeightConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double width && width > 0)
                {
                    // Bed32View 原始尺寸 800x450，比例 450/800 = 0.5625
                    return width * 0.5625;
                }
                return 0;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        public class BedCardTemplateSelector : DataTemplateSelector
        {
            public DataTemplate Template16 { get; set; }
            public DataTemplate Template8 { get; set; }
            public DataTemplate TemplateDetailLeft { get; set; }
        
            public override DataTemplate SelectTemplate(object item, DependencyObject container)
            {
                // 向上查找 ItemsControl
                var current = container;
                while (current != null && !(current is ItemsControl))
                    current = VisualTreeHelper.GetParent(current);
        
                if (current is ItemsControl itemsControl)
                {
                    var viewModel = itemsControl.DataContext as WarmBoxViewModel;
                    if (viewModel != null)
                    {
                        if (viewModel.IsDetailMode)
                            return TemplateDetailLeft;
                        else
                            return viewModel.CurrentViewMode == 16 ? Template16 : Template8;
                    }
                }
        
                return base.SelectTemplate(item, container);
            }
        }

    public class BoolInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // 如果参数为 "true" 则返回原值，否则取反
                if (parameter?.ToString() == "true")
                    return boolValue;
                else
                    return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter?.ToString() == "true")
                    return boolValue;
                else
                    return !boolValue;
            }
            return false;
        }
    }


}



