using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Services;
using WarmBox_Central_Monitoring_Station.View.SetIndexs;

namespace WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs
{
    public class FactoryModeViewModel : INotifyPropertyChanged
    {
        private readonly IServiceDataService _serviceDataService;
        private string _password;
        private bool _isAuthenticated;
        private object _currentContent;

        public FactoryModeViewModel(IServiceDataService serviceDataService)
        {
            _serviceDataService = serviceDataService;
            VerifyCommand = new RelayCommand(VerifyPassword);
            // 初始显示登录界面
            CurrentContent = new FactoryModeLoginView();
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set { _isAuthenticated = value; OnPropertyChanged(); }
        }

        public object CurrentContent
        {
            get => _currentContent;
            set { _currentContent = value; OnPropertyChanged(); }
        }

        public ICommand VerifyCommand { get; }

        private void VerifyPassword()
        {
            if (Password == "8888")
            {
                IsAuthenticated = true;
                var settingsView = new FactoryModeSettingsView();
                var settingsViewModel = new FactoryModeSettingsViewModel(_serviceDataService);
                settingsView.DataContext = settingsViewModel;
                CurrentContent = settingsView;
            }
            else
            {
                string message = Application.Current.TryFindResource("VerifyPasswordFailed") as string ?? "密码错误，请重试。";
                string title = Application.Current.TryFindResource("VerifyPasswordTitle") as string ?? "验证失败";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                Password = string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // 简单命令实现
  
}
