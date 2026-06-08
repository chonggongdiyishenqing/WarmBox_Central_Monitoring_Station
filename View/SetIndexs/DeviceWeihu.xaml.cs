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
using WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs;

namespace WarmBox_Central_Monitoring_Station.View.SetIndexs
{
    /// <summary>
    /// DeviceWeihu.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceWeihu : UserControl
    {
        public DeviceWeihu()
        {
            InitializeComponent();
            this.Unloaded += UserControl_Unloaded;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DeviceWeihuViewModel vm)
            {
                vm.Dispose();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"DataContext 类型: {DataContext?.GetType()}");

            }
        }
    }
}
