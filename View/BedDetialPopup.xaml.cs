using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.ViewModel;

namespace WarmBox_Central_Monitoring_Station.View
{
    public partial class BedDetialPopup : UserControl
    {
        private BedDetailViewModel _vm;
        public BedDetialPopup()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 查找父级 WarmBoxViewModel 并执行关闭命令
            DependencyObject current = this;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext is ViewModel.WarmBoxViewModel vm)
                {
                    vm.CloseDetailCommand.Execute(null);
                    break;
                }
                current = VisualTreeHelper.GetParent(current);
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is BedDetailViewModel oldVm)
                oldVm.PrintTriggered -= OnPrintTriggered;

            if (e.NewValue is BedDetailViewModel newVm)
            {
                _vm = newVm;
                _vm.PrintTriggered += OnPrintTriggered;
            }
        }

      private void OnPrintTriggered()
{
    if (ContentControl.Content is FrameworkElement waveformView)
    {
        // 开始打印
        string printingMsg = Application.Current.TryFindResource("Printing") as string ?? "正在打印...";
        ToastHelper.Show(printingMsg, "#FFFFFF", 1500);

        bool success = PrintHelper.PrintUIElement(waveformView, "监护波形");

        string msg, color;
        if (success)
        {
            msg = Application.Current.TryFindResource("PrintCompleted") as string ?? "打印完成";
            color = "#44FF44";
        }
        else
        {
            msg = Application.Current.TryFindResource("PrintFailed") as string ?? "打印失败";
            color = "#FF4444";
        }
        ToastHelper.Show(msg, color);
    }
    else
    {
        string msg = Application.Current.TryFindResource("NoWaveformView") as string ?? "无可打印的波形视图";
        ToastHelper.Show(msg, "#FF4444");
    }
}


    }
}