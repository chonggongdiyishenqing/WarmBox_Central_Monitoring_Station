using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Services;
using WarmBox_Central_Monitoring_Station.View;
using System.Diagnostics;

namespace WarmBox_Central_Monitoring_Station.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceDataService _serviceDataService;
        private readonly IProcessService _processService;
        private string _username;
        private string _password;
        private bool _rememberPassword;
        private Window _loginWindow;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public bool RememberPassword
        {
            get => _rememberPassword;
            set
            {
                _rememberPassword = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand ExitCommand { get; }

        public LoginViewModel(IServiceProvider serviceProvider, IServiceDataService serviceDataService, IProcessService processService, Window loginWindow = null)
        {
            _processService = processService;
            LoginCommand = new RelayCommand(async (parameter) => await ExecuteLoginAsync(parameter));
            ExitCommand = new RelayCommand(ExecuteExit);
            _serviceProvider = serviceProvider;
            _serviceDataService = serviceDataService;
            _loginWindow = loginWindow;

            // 加载保存的凭据
            LoadSavedCredentials();
        }

        // 构造函数调用的方法，加载保存的凭据
        private async void LoadSavedCredentials()
        {
            try
            {
                // 从数据库获取记住的用户
                var rememberedUser = await _serviceDataService.GetRememberedUserAsync();

                if (rememberedUser.HasValue)
                {
                    Username = rememberedUser.Value.username;
                    Password = rememberedUser.Value.password;
                    RememberPassword = true;

                    Console.WriteLine($"✅ 已加载保存的凭据: {Username}");
                }
                else
                {
                    Console.WriteLine("ℹ️ 没有找到保存的凭据");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 加载保存的凭据时出错: {ex.Message}");
            }
        }

        // 异步登录方法
        private async Task ExecuteLoginAsync(object parameter)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("账户和密码不可为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 显示加载状态
                Mouse.OverrideCursor = Cursors.Wait;

                // 验证用户
                bool isValid = await _serviceDataService.ValidateUserAsync(Username, Password);

                if (isValid)
                {
                    // 处理记住密码逻辑
                    await HandleRememberPassword();


                    // 登录成功，进入主界面
                    ShowMainWindow();

                    StartServiceAsync();

                    await Task.Delay(100);
                  

                }
                else
                {
                    MessageBox.Show("用户名或密码错误", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"登录时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"❌ 登录错误: {ex.Message}");
            }
            finally
            {
                // 恢复光标
                Mouse.OverrideCursor = null;
            }
        }


        private async Task StartServiceAsync()
        {
            try
            {
                string processName = "WarmBox_Date_AcceptSend";  // 进程名，不含.exe
                Process[] existingProcesses = Process.GetProcessesByName(processName);
                if (existingProcesses.Length > 0)
                {
                    Console.WriteLine($"{processName}.exe 已在运行，不再重复启动");
                    return;
                }

                // 获取项目程序目录的上级目录（Davids目录）
                string currentProjectPath = AppDomain.CurrentDomain.BaseDirectory;//login这里调试的时候要改回去
                DirectoryInfo projectDir = new DirectoryInfo(currentProjectPath);

                // 向上找到Davids目录
                while (projectDir != null && projectDir.Name != "WarmBox_Central_Monitoring_Station")
                {
                    projectDir = projectDir.Parent;
                }

                if (projectDir?.Parent == null)
                {
                    MessageBox.Show("找不到WarmBox程序目录", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string davidsRoot = projectDir.Parent.FullName;
                string resolvePath = Path.Combine(davidsRoot, "WarmBox_Date_AcceptSend", "bin", "Debug", "net8.0","WarmBox_Date_AcceptSend.exe");  //"改回来加/*bin", "Debug", "net8.0" */,

                // 检查Resolve.exe文件是否存在
                if (!File.Exists(resolvePath))
                {
                    MessageBox.Show($"WarmBox.exe文件不存在，{resolvePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 使用进程服务启动Resolve.exe
                bool started = await _processService.StartProcessAsync(resolvePath);

                if (started)
                {
                    Console.WriteLine("✅ WarmBox.exe 已启动");
                }
                else
                {
                    MessageBox.Show("启动WarmBox.exe失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动服务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    
        private async Task HandleRememberPassword()
        {
            if (RememberPassword)
            {
                // 保存凭据到数据库
                bool saved = await _serviceDataService.UpdateRememberedUserAsync(Username, Password);
                if (saved)
                {
                    Console.WriteLine($"✅ 已保存凭据: {Username}");
                }
                else
                {
                    Console.WriteLine("❌ 保存凭据失败");
                }
            }
            else
            {
                // 清除记住的凭据
                bool cleared = await _serviceDataService.ClearRememberedUserAsync();
                if (cleared)
                {
                    Console.WriteLine("✅ 已清除保存的凭据");
                }
            }
        }

        // 显示主窗口
        private void ShowMainWindow()
        {
            var indexView = _serviceProvider.GetService<IndexView>();
            var indexViewModel = _serviceProvider.GetService<IndexViewModel>();
            indexView.DataContext = indexViewModel;

            //// 设置App的主ViewModel
            App.SetMainViewModel(indexViewModel);

            // 显示主界面
            indexView.Show();

            // 关闭登录窗口
            CloseLoginWindow();
        }

        private void CloseLoginWindow()
        {
            if (_loginWindow != null)
            {
                _loginWindow.Close();
            }
            else
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginView loginView)
                    {
                        loginView.Close();
                        break;
                    }
                }
            }
        }

        private void ExecuteExit(object parameter)
        {
            Application.Current.Shutdown();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
