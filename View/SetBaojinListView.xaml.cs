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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WarmBox_Central_Monitoring_Station.View
{
    /// <summary>
    /// SetBaojinListView.xaml 的交互逻辑
    /// </summary>
    public partial class SetBaojinListView : Window
    {
        // 新增：一个接收起始点坐标的构造函数
        public SetBaojinListView()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        //// 原有的无参构造函数可以保留，但建议内部调用新的
        //public SetBaojinListView() : this(new Point(0, 0))
        //{
        //    // 默认构造函数，如果不传点，则从(0,0)开始（可能从屏幕左上角弹出）
        //}

        private void StartScaleFromPointAnimation(Point startPointOnScreen)
        {
            // 0. 准备工作：设置窗口初始状态
            this.Opacity = 0;
            this.WindowStartupLocation = WindowStartupLocation.Manual; // 必须改为手动定位

            // 1. 计算窗口的最终目标位置（例如屏幕中央）
            double targetLeft = (SystemParameters.WorkArea.Width - this.Width) / 2;
            double targetTop = (SystemParameters.WorkArea.Height - this.Height) / 2;
            this.Left = targetLeft;
            this.Top = targetTop;

            // 2. 核心计算：动画需要移动的“差值”
            // 动画的“From”值是：从起点到当前位置的偏移量
            // 因为窗口已经定位在目标位置(targetLeft, targetTop)，而起点是startPointOnScreen
            double fromX = startPointOnScreen.X - targetLeft;
            double fromY = startPointOnScreen.Y - targetTop;

            // 3. 获取并配置动画
            Storyboard storyboard = (Storyboard)this.FindResource("ScaleFromPointStoryboard");

            // 找到XAML中定义的动画，并动态设置其起始值
            DoubleAnimation animX = storyboard.Children[3] as DoubleAnimation; // 第4个是TranslateX
            DoubleAnimation animY = storyboard.Children[4] as DoubleAnimation; // 第5个是TranslateY

            if (animX != null) animX.From = fromX;
            if (animY != null) animY.From = fromY;

            // 动画的“To”值固定是0，因为最终要回到原始位置（即窗口自身坐标）
            if (animX != null) animX.To = 0;
            if (animY != null) animY.To = 0;

            // 4. 开始动画！
            storyboard.Begin(this);
        }


        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
