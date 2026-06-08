using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarmBox_Central_Monitoring_Station.Services
{
    public class ProcessService : IProcessService, IDisposable
    {
        // 用于跟踪管理的进程
        private readonly ConcurrentDictionary<string, Process> _managedProcesses = new();
        private bool _disposed = false;

        // 启动进程并管理它
        public async Task<bool> StartProcessAsync(string processPath, string arguments = null)
        {
            try
            {
                // 检查文件是否存在
                if (!File.Exists(processPath))
                {
                    Console.WriteLine($"❌ 进程文件不存在: {processPath}");
                    return false;
                }

                string processName = Path.GetFileNameWithoutExtension(processPath);

                // 如果进程已经在运行，先停止它
                if (_managedProcesses.ContainsKey(processName) &&
                    !_managedProcesses[processName].HasExited)
                {
                    Console.WriteLine($"⚠️ 进程 {processName} 已在运行，正在停止...");
                    await StopProcessAsync(processName);
                }

                // 启动新进程
                var startInfo = new ProcessStartInfo
                {
                    FileName = processPath,
                    WorkingDirectory = Path.GetDirectoryName(processPath),
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = false
                };

                if (!string.IsNullOrEmpty(arguments))
                {
                    startInfo.Arguments = arguments;
                }

                var process = Process.Start(startInfo);

                if (process != null)
                {
                    // 订阅进程退出事件
                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, e) =>
                    {
                        Console.WriteLine($"ℹ️ 进程 {processName} 已退出");
                        _managedProcesses.TryRemove(processName, out _);
                    };

                    // 添加到管理列表
                    _managedProcesses[processName] = process;

                    Console.WriteLine($"✅ 已启动进程: {processName} (PID: {process.Id})");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 启动进程失败: {ex.Message}");
                return false;
            }
        }

        // 停止指定进程
        public async Task<bool> StopProcessAsync(string processName)
        {
            try
            {
                if (_managedProcesses.TryGetValue(processName, out var process))
                {
                    if (!process.HasExited)
                    {
                        Console.WriteLine($"⏹️ 正在停止进程: {processName}");

                        // 先尝试优雅关闭
                        process.CloseMainWindow();

                        // 等待一段时间
                        //await Task.Delay(500);

                        // 如果还没退出，强制终止
                        if (!process.HasExited)
                        {
                            process.Kill();
                            await process.WaitForExitAsync();
                        }

                        Console.WriteLine($"✅ 已停止进程: {processName}");
                    }

                    _managedProcesses.TryRemove(processName, out _);
                    return true;
                }

                // 如果不在管理列表中，尝试通过进程名查找
                var processes = Process.GetProcessesByName(processName);
                if (processes.Any())
                {
                    Console.WriteLine($"⚠️ 进程 {processName} 在运行但不在管理列表中");
                    foreach (var proc in processes)
                    {
                        try
                        {
                            proc.Kill();
                            await proc.WaitForExitAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ 停止进程 {processName} 失败: {ex.Message}");
                        }
                    }
                    return true;
                }

                Console.WriteLine($"ℹ️ 进程 {processName} 未在运行");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 停止进程失败: {ex.Message}");
                return false;
            }
        }

        // 检查进程是否在运行
        public bool IsProcessRunning(string processName)
        {
            if (_managedProcesses.TryGetValue(processName, out var process))
            {
                return !process.HasExited;
            }

            return Process.GetProcessesByName(processName).Any();
        }

        // 获取管理的进程
        public Process GetProcess(string processName)
        {
            _managedProcesses.TryGetValue(processName, out var process);
            return process;
        }

        // 停止所有管理的进程
        public async Task StopAllProcessesAsync()
        {
            Console.WriteLine($"🛑 正在停止所有管理的进程...");

            var processNames = _managedProcesses.Keys.ToList();

            foreach (var processName in processNames)
            {
                await StopProcessAsync(processName);
            }

            Console.WriteLine($"✅ 已停止所有进程");
        }

        // 实现IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 停止所有进程
                    StopAllProcessesAsync().Wait();

                    // 清理资源
                    foreach (var process in _managedProcesses.Values)
                    {
                        process?.Dispose();
                    }
                    _managedProcesses.Clear();
                }
                _disposed = true;
            }
        }
    }
}
