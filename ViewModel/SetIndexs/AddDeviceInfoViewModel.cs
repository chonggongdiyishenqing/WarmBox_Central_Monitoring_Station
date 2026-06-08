using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.Services;
using static WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs.DeviceWeihuViewModel;

namespace WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs
{
    public class AddDeviceInfoViewModel : INotifyPropertyChanged
    {
        private readonly IServiceDataService _serviceDataService;
        private Device _editingDevice;
        private bool _isReadOnly;

        private const string DEFAULT_BRAND = "戴维";
        private const string DEFAULT_TYPE = "暖箱";
        private const string DEFAULT_MODEL = "YP-3100";

        public AddDeviceInfoViewModel(IServiceDataService serviceDataService, Device editingDevice = null, bool isReadOnly = false)
        {
            _serviceDataService = serviceDataService;
            _editingDevice = editingDevice;
            _isReadOnly = isReadOnly;

            InitializeFixedData();
            InitializeCommands();

            // 设置窗口标题
            SetWindowTitle();

            if (_editingDevice != null)
            {
                DeviceNumber = _editingDevice.Device_Num;
                IPAddress = _editingDevice.Device_ip;
                SelectedBrand = _editingDevice.Pingpai;
                SelectedDeviceType = _editingDevice.Device_type;
                SelectedDeviceModel = _editingDevice.Device_Name;
            }
            else
            {
                SelectedBrand = DEFAULT_BRAND;
                SelectedDeviceType = DEFAULT_TYPE;
                SelectedDeviceModel = DEFAULT_MODEL;
            }
        }

        #region Properties

        private ObservableCollection<string> _brands;
        public ObservableCollection<string> Brands { get => _brands; set { _brands = value; OnPropertyChanged(); } }

        private string _selectedBrand;
        public string SelectedBrand { get => _selectedBrand; set { _selectedBrand = value; OnPropertyChanged(); } }

        private ObservableCollection<string> _deviceTypes;
        public ObservableCollection<string> DeviceTypes { get => _deviceTypes; set { _deviceTypes = value; OnPropertyChanged(); } }

        private string _selectedDeviceType;
        public string SelectedDeviceType { get => _selectedDeviceType; set { _selectedDeviceType = value; OnPropertyChanged(); } }

        private ObservableCollection<string> _deviceModels;
        public ObservableCollection<string> DeviceModels { get => _deviceModels; set { _deviceModels = value; OnPropertyChanged(); } }

        private string _selectedDeviceModel;
        public string SelectedDeviceModel { get => _selectedDeviceModel; set { _selectedDeviceModel = value; OnPropertyChanged(); } }

        private string _deviceNumber;
        public string DeviceNumber { get => _deviceNumber; set { _deviceNumber = value; OnPropertyChanged(); } }

        private string _ipAddress;
        public string IPAddress { get => _ipAddress; set { _ipAddress = value; OnPropertyChanged(); } }

        public string Department => "设备科"; // 固定中文，但可改为资源

        public bool IsReadOnly { get => _isReadOnly; set { _isReadOnly = value; OnPropertyChanged(); } }

        private string _windowTitle;
        public string WindowTitle { get => _windowTitle; set { _windowTitle = value; OnPropertyChanged(); } }

        #endregion

        #region Commands

        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            CancelCommand = new RelayCommand(() => CloseWindow?.Invoke());
        }

        #endregion

        #region Methods

        private void InitializeFixedData()
        {
            Brands = new ObservableCollection<string> { DEFAULT_BRAND };
            DeviceTypes = new ObservableCollection<string> { DEFAULT_TYPE };
            DeviceModels = new ObservableCollection<string> { DEFAULT_MODEL };
        }

        private void SetWindowTitle()
        {
            string resourceKey;
            if (_editingDevice != null)
                resourceKey = _isReadOnly ? "ViewDeviceTitle" : "EditDeviceTitle";
            else
                resourceKey = "AddDeviceTitle";

            WindowTitle = GetResourceString(resourceKey);
        }

        private static string GetResourceString(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }

        private async Task SaveAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(DeviceNumber))
                {
                    MessageBox.Show(GetResourceString("EnterDeviceNumber"), GetResourceString("Warning"));
                    return;
                }
                if (string.IsNullOrWhiteSpace(IPAddress))
                {
                    MessageBox.Show(GetResourceString("EnterIPAddress"), GetResourceString("Warning"));
                    return;
                }

                string ipPattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
                if (!Regex.IsMatch(IPAddress, ipPattern))
                {
                    MessageBox.Show(GetResourceString("InvalidIP"), GetResourceString("Warning"));
                    return;
                }

                if (_editingDevice != null)
                {
                    var device = new Device
                    {
                        ID = _editingDevice.ID,
                        Device_Num = DeviceNumber,
                        Pingpai = SelectedBrand,
                        Device_type = SelectedDeviceType,
                        Device_Name = SelectedDeviceModel,
                        Device_ip = IPAddress,
                        Device_keshi = Department
                    };
                    var success = await _serviceDataService.UpdateDeviceAsync(device);
                    if (success.Success)
                    {
                        MessageBox.Show(GetResourceString("DeviceUpdated"), GetResourceString("Success"));
                        BedConfigEventPublisher.NotifyBedConfigChanged();
                        CloseWindow?.Invoke();
                    }
                    else
                    {
                        MessageBox.Show(success.Message);
                    }
                }
                else
                {
                    var device = new Device
                    {
                        Device_Num = DeviceNumber,
                        Pingpai = SelectedBrand,
                        Device_type = SelectedDeviceType,
                        Device_Name = SelectedDeviceModel,
                        Device_ip = IPAddress,
                        Device_keshi = Department
                    };
                    var success = await _serviceDataService.AddDeviceAsync(device);
                    if (success.Success)
                    {
                        MessageBox.Show(GetResourceString("DeviceAdded"), GetResourceString("Success"));
                        BedConfigEventPublisher.NotifyBedConfigChanged();
                        CloseWindow?.Invoke();
                    }
                    else
                    {
                        MessageBox.Show(success.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetResourceString("SaveFailed") + ex.Message, GetResourceString("Error"));
            }
        }

        public Action CloseWindow { get; set; }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}