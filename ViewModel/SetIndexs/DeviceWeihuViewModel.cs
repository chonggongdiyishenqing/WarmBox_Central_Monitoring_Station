using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.Services;
using WarmBox_Central_Monitoring_Station.View.SetIndexs;

namespace WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs
{
    public class DeviceWeihuViewModel : INotifyPropertyChanged
    {
        private readonly IServiceDataService _serviceDataService;
        private ObservableCollection<Device> _device;
        private int _currentPage = 1;
        private int _pageSize = 15;
        private int _totalCount;
        private string _searchTextsb;
        private string _searchTextip;
        public DeviceWeihuViewModel(IServiceDataService serviceDataService)
        {
            _serviceDataService = serviceDataService;
            BedConfigEventPublisher.BedConfigChanged += OnBedConfigChanged;
            InitializeCommands();
            LoadAsync();
        }

        #region Properties

        public ObservableCollection<Device> Devices
        {
            get => _device;
            set { _device = value; OnPropertyChanged(); }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public int PageSize
        {
            get => _pageSize;
            set { _pageSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPages)); }
        }

        public int TotalCount
        {
            get => _totalCount;
            set { _totalCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPages)); }
        }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public string SearchText_sb
        {
            get => _searchTextsb;
            set { _searchTextsb = value; OnPropertyChanged(); }
        }

        public string SearchText_ip
        {
            get => _searchTextip;
            set { _searchTextip = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public RelayCommand LoadCommand { get; private set; }
        public RelayCommand AddCommand { get; private set; }
        public RelayCommand SearchCommand { get; private set; }
        public RelayCommand NextPageCommand { get; private set; }
        public RelayCommand PreviousPageCommand { get; private set; }
        public RelayCommand<Device> EditDeviceCommand { get; private set; }
        public RelayCommand<Device> DeleteDeviceCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadCommand = new RelayCommand(async () => await LoadAsync());
            AddCommand = new RelayCommand(ExecuteAdd);
            SearchCommand = new RelayCommand(async () => await SearchAsync());
            NextPageCommand = new RelayCommand(NextPage);
            PreviousPageCommand = new RelayCommand(PreviousPage);
            EditDeviceCommand = new RelayCommand<Device>(ExecuteEdit);
            DeleteDeviceCommand = new RelayCommand<Device>(ExecuteDelete);
        }

        #endregion

        #region Methods

        private async Task LoadAsync()
        {
            try
            {
                TotalCount = await _serviceDataService.GetTotalCountAsync("Device");
                var devices = await _serviceDataService.GetDevicesByPageAsync(CurrentPage, PageSize, "Device");
                Devices = new ObservableCollection<Device>(devices);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载设备列表失败: {ex.Message}");
            }
        }

        private async Task SearchAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText_ip) && string.IsNullOrWhiteSpace(SearchText_sb))
                {
                    await LoadAsync();
                    MessageBox.Show("至少输入一个搜索条件");
                    return;
                }

                var devices = await _serviceDataService.SearchDevicesAsync(SearchText_sb, SearchText_ip);
                Devices = new ObservableCollection<Device>(devices);
                TotalCount = devices.Count();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"搜索失败: {ex.Message}");
            }
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                _ = LoadAsync();
            }
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                _ = LoadAsync();
            }
        }
        private void OnBedConfigChanged()
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                await Task.Delay(50);
                LoadAsync();
            });
        }
        private void ExecuteAdd()
        {
            var addWindow = App.ServiceProvider.GetRequiredService<AddDeviceinform>();
            var viewModel = new AddDeviceInfoViewModel(_serviceDataService, null, false);
            viewModel.CloseWindow = () => addWindow.Close();

            addWindow.DataContext = viewModel;
            //addWindow.Owner = Application.Current.MainWindow;
            addWindow.ShowDialog();

            //_ = LoadAsync();
        }

        private void ExecuteEdit(Device device)
        {
            if (device == null) return;

            var editWindow = App.ServiceProvider.GetRequiredService<AddDeviceinform>();
            var viewModel = new AddDeviceInfoViewModel(_serviceDataService,device, false);
            viewModel.CloseWindow = () => editWindow.Close();

            editWindow.DataContext = viewModel;
            //editWindow.Owner = Application.Current.MainWindow;
            editWindow.ShowDialog();

            //_ = LoadAsync();
        }

        private void ExecuteDelete(Device device)
        {
            if (device == null) return;

            var confirmMsg = Application.Current.TryFindResource("ConfirmDeleteDevice") as string ?? "确定要删除该设备吗？";
            var title = Application.Current.TryFindResource("Warning") as string ?? "警告";
            var result = MessageBox.Show(confirmMsg, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _ = DeleteDeviceAsync(Convert.ToInt32(device.ID));
            }
        }

        private async Task DeleteDeviceAsync(int deviceId)
        {
            try
            {
                var success = await _serviceDataService.DeleteDeviceAsync(deviceId);
                if (success.Success)
                {
                    var deletedMsg = Application.Current.TryFindResource("DeviceDeleted") as string ?? "设备已删除";
                    var successTitle = Application.Current.TryFindResource("Success") as string ?? "成功";
                    MessageBox.Show(deletedMsg, successTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                    BedConfigEventPublisher.NotifyBedConfigChanged();
                    _ = LoadAsync();
                }
                else
                {
                    var failMsg = Application.Current.TryFindResource("DeviceDeleteFailed") as string ?? "设备删除失败！";
                    var errorTitle = Application.Current.TryFindResource("Error") as string ?? "错误";
                    MessageBox.Show(failMsg, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                var errorPrefix = Application.Current.TryFindResource("DeleteDeviceError") as string ?? "删除设备失败: ";
                MessageBox.Show(errorPrefix + ex.Message,
                    Application.Current.TryFindResource("Error") as string ?? "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 查看设备详情（只读模式）


        #endregion
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            // 取消事件订阅
            BedConfigEventPublisher.BedConfigChanged -= OnBedConfigChanged;
            Console.WriteLine("已取消订阅");
        }

        public class Device
        {
            public string ID { get; set; }
            public string Device_ip { get; set; }
            public string Device_Num { get; set; }
            public string Pingpai { get; set; }          // 对应 Device_pingpai
            public string Device_type { get; set; }      // 对应 Device_Type
            public string Device_keshi { get; set; }
            public string Device_Name { get; set; }
 
        }
    }
}
