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

namespace WarmBox_Central_Monitoring_Station.View
{
    /// <summary>
    /// LoginView.xaml 的交互逻辑
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void TitlePanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // 如果文本是提示文本，清空
                if (textBox.Text == "请输入账号")
                {
                    textBox.Text = "";
                    textBox.Foreground = System.Windows.Media.Brushes.White;
                }
            }
        }

        private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // 如果文本框为空，显示提示文本
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = "请输入账号";
                    textBox.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // 隐藏提示文本
            PasswordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                // 如果密码框为空，显示提示文本
                if (string.IsNullOrEmpty(passwordBox.Password))
                {
                    PasswordPlaceholder.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
