using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WarmBox_Central_Monitoring_Station.View.SetIndexs;
using WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs;
using WarmBox_Central_Monitoring_Station.Services; // 引入服务接口命名空间

namespace WarmBox_Central_Monitoring_Station.ViewModel
{
    public class SetIndexViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<MenuItem> _menuItems;
        private MenuItem _selectedMenuItem;
        private object _currentView;
        private readonly IServiceProvider _serviceProvider;

        public SetIndexViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            // 定义菜单项时使用资源键（不再是最终显示文本）
            var menuKeys = new[] { "PatientManagement", "AboutSystem", "ManufacturerMode", "LanguageConfig" };
            var viewTypes = new[] { typeof(PatientManagementView), typeof(AboutSystemView), typeof(FactoryModeView), typeof(LanguageSetView) };
            var vmTypes = new[] { typeof(PatientManagementViewModel), typeof(AboutSystemViewModel), typeof(FactoryModeViewModel), typeof(LanguageSetViewModel) };

            MenuItems = new ObservableCollection<MenuItem>();
            for (int i = 0; i < menuKeys.Length; i++)
            {
                MenuItems.Add(new MenuItem
                {
                    Name = GetResourceString(menuKeys[i]),  // 初始显示文本
                    ViewType = viewTypes[i],
                    ViewModelType = vmTypes[i],
                    Tag = menuKeys[i]                       // 保存资源键，用于后续刷新
                });
            }

            SelectedMenuItem = MenuItems[0];

            // 订阅语言切换事件，假设 LanguageService 有静态事件
            LanguageService.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(string cultureName)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            foreach (var item in MenuItems)
            {
                if (item.Tag is string resourceKey)
                {
                    item.Name = GetResourceString(resourceKey);
                }
            }
            // 更新顶部标题（因为 SelectedMenuItem 的 Name 可能变了）
            OnPropertyChanged(nameof(SelectedMenuItem));
        }

        private string GetResourceString(string key)
        {
            // 从应用程序资源中查找，若未找到则返回 key 本身
            return Application.Current.TryFindResource(key) as string ?? key;
        }

        public ObservableCollection<MenuItem> MenuItems
        {
            get => _menuItems;
            set { _menuItems = value; OnPropertyChanged(); }
        }

        public MenuItem SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (_selectedMenuItem != value)
                {
                    _selectedMenuItem = value;
                    OnPropertyChanged();
                    if (_selectedMenuItem?.ViewType != null)
                    {
                        var view = Activator.CreateInstance(_selectedMenuItem.ViewType);
                        if (view is FrameworkElement element)
                        {
                            object viewModel = null;
                            if (_selectedMenuItem.ViewModelType != null)
                            {
                                var service = _serviceProvider.GetService(typeof(IServiceDataService)) as IServiceDataService;

                                // 特殊处理需要多个参数的 ViewModel
                                if (_selectedMenuItem.ViewModelType == typeof(PatientManagementViewModel))
                                {
                                    viewModel = Activator.CreateInstance(
                                        _selectedMenuItem.ViewModelType,
                                        service,
                                        _serviceProvider);
                                }
                                else
                                {
                                    try
                                    {
                                        viewModel = Activator.CreateInstance(_selectedMenuItem.ViewModelType, service);
                                    }
                                    catch
                                    {
                                        viewModel = Activator.CreateInstance(_selectedMenuItem.ViewModelType);
                                    }
                                }
                                element.DataContext = viewModel;
                            }
                        }
                        CurrentView = view;
                    }
                }
            }
        }

        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MenuItem : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }
        public Type ViewType { get; set; }
        public Type ViewModelType { get; set; }

        public object Tag { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}