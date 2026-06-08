using Microsoft.Extensions.DependencyInjection;
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
using System.Windows.Shapes;
using WarmBox_Central_Monitoring_Station.ViewModel;

namespace WarmBox_Central_Monitoring_Station.View
{
    /// <summary>
    /// IndexView.xaml 的交互逻辑
    /// </summary>
    public partial class IndexView : Window
    {
        private readonly IServiceProvider _serviceProvider;
        public IndexView(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            this.Loaded += (s, e) =>
            {
                this.WindowState = WindowState.Maximized;
            };
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 拖动窗口
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void wu1_Click(object sender, RoutedEventArgs e)
        {
            //1.获取按钮在屏幕上的绝对位置（这是关键！）
            Point buttonLocation = wu1.PointToScreen(new Point(0, 0));

            // 2. 创建窗口，传入起始点
            var setbaojinlistview = new SetBaojinListView();
            setbaojinlistview.DataContext = new SetBaojinListViewModel();
          

            // 3. 显示窗口
            setbaojinlistview.ShowDialog();
        }

        private void min1_Click(object sender, RoutedEventArgs e)
        {
            Point buttonLocation = wu1.PointToScreen(new Point(0, 0));
            // 2. 创建窗口，传入起始点
            var setview = new SetIndex();       
            setview.DataContext = new SetIndexViewModel(_serviceProvider);
          
            // 3. 显示窗口
            setview.ShowDialog();
        }
    }
}
