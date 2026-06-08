using HandyControl.Controls;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WarmBox_Central_Monitoring_Station.View.BedDetialPopupUsercontroles
{
    /// <summary>
    /// CameraConnectingView.xaml 的交互逻辑
    /// </summary>
    public partial class CameraConnectingView : UserControl
    {
        public CameraConnectingView()
        {
            InitializeComponent();
            //this.Loaded += OnLoaded;
        }

        //private void OnLoaded(object sender, RoutedEventArgs e)
        //{
        //    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        //    string jsonPath = System.IO.Path.Combine(baseDir, "IMG", "Animations", "camera.json");

        //    if (System.IO.File.Exists(jsonPath))
        //    {
        //        LottieView.ResourcePath = "pack://application:,,,/IMG/Animations/camera.json";
        //        LottieView.RepeatCount = -1;   // 无限循环
        //        LottieView.AutoPlay = true;    // 自动播放
        //    }
        //    else
        //    {
        //        System.Diagnostics.Debug.WriteLine("动画文件未找到: " + jsonPath);
        //    }
        //}

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog();
        }

        private void CloseDialog()
        {
            System.Windows.Window.GetWindow(this)?.Close();
        }
    }
}
