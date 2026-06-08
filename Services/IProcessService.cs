using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarmBox_Central_Monitoring_Station.Services
{
    public interface IProcessService
    {
        // 启动进程
        Task<bool> StartProcessAsync(string processPath, string arguments = null);

        // 停止进程
        Task<bool> StopProcessAsync(string processName);

        // 检查进程是否在运行
        bool IsProcessRunning(string processName);

        // 获取进程
        Process GetProcess(string processName);

        // 停止所有管理的进程
        Task StopAllProcessesAsync();
    }
}
