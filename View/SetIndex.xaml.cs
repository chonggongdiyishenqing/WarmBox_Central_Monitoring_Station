using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace WarmBox_Central_Monitoring_Station.View
{
    public partial class SetIndex : Window
    {
        public SetIndex()
        {
            InitializeComponent();
            // 使窗口在屏幕中央显示
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        // 若需要使用从指定点展开的动画，可调用此构造函数并传入起点
        

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        //private void StartScaleFromPointAnimation(Point startPointOnScreen)
        //{
        //    this.Opacity = 0;
        //    // 计算起点相对于窗口位置的偏移
        //    double fromX = startPointOnScreen.X - this.Left;
        //    double fromY = startPointOnScreen.Y - this.Top;

        //    Storyboard storyboard = (Storyboard)this.FindResource("ScaleFromPointStoryboard");
        //    if (storyboard != null)
        //    {
        //        DoubleAnimation animX = storyboard.Children[3] as DoubleAnimation;
        //        DoubleAnimation animY = storyboard.Children[4] as DoubleAnimation;
        //        if (animX != null) animX.From = fromX;
        //        if (animY != null) animY.From = fromY;
        //        if (animX != null) animX.To = 0;
        //        if (animY != null) animY.To = 0;
        //        storyboard.Begin(this);
        //    }
        //}

        private void NavListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 绑定已处理，此方法可留空
        }
    }
}