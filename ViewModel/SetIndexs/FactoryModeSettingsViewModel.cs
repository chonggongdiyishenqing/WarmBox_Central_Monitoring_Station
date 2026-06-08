using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WarmBox_Central_Monitoring_Station.Services;
using WarmBox_Central_Monitoring_Station.View.SetIndexs;

namespace WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs
{
    public class FactoryModeSettingsViewModel : INotifyPropertyChanged
    {
        private readonly IServiceDataService _serviceDataService;
        private UserControl _currentView;
        public UserControl CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
                SwitchView(value);
            }
        }
        public FactoryModeSettingsViewModel(IServiceDataService serviceDataService)
        {
            _serviceDataService = serviceDataService;
            SelectedTabIndex = 0;
        }
        private void SwitchView(int tabIndex)
        {
            switch (tabIndex)
            {
                case 0: // 设备维护
                        // 解析 ViewModel（容器会自动注入 IServiceDataService）
                    var deviceVm = App.ServiceProvider.GetRequiredService<DeviceWeihuViewModel>();
                    // 解析 View
                    var deviceView = App.ServiceProvider.GetRequiredService<DeviceWeihu>();
                    // 绑定 DataContext
                    deviceView.DataContext = deviceVm;
                    CurrentView = deviceView;
                    break;
                case 1: // 网络配置
                    var networkVm = App.ServiceProvider.GetRequiredService<NetWorkSetViewModel>();
                    var networkView = App.ServiceProvider.GetRequiredService<NetWorkSetView>();
                    networkView.DataContext = networkVm;
                    CurrentView = networkView;
                    break;
                default:
                    CurrentView = new UserControl();
                    break;
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
