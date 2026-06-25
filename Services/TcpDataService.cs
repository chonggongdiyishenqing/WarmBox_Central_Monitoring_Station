using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WarmBox_Central_Monitoring_Station.Services
{
    public class TcpDataService : IDisposable
    {
        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;
        private readonly string _listenIp;      // 新增
        private readonly int _port;             // 保留

        // 用于管理所有活跃的客户端处理任务
        private List<Task> _activeClientTasks = new List<Task>();
        private readonly object _clientsLock = new object();

        public event Action<string> OnDataReceived;
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;

        // 设备数据缓存（按设备IP分组）
        private readonly Dictionary<string, StringBuilder> _deviceDataBuffers = new Dictionary<string, StringBuilder>();
        private readonly object _bufferLock = new object();

        // 新增：直接接收原始 HL7 消息的事件
        public event Action<string, string> OnHl7MessageReceived; // 参数 (deviceIp, hl7Message)

        private async Task HandleClientAsync(TcpClient client)
        {
            var clientEndPoint = client.Client.RemoteEndPoint.ToString();
            Console.WriteLine($"客户端连接: {clientEndPoint}");
            string clientIp = GetClientIp(client);

            // 每个客户端一个消息缓冲区
            var dataBuffer = new StringBuilder();

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    byte[] buffer = new byte[4096];
                    while (!_cancellationTokenSource.Token.IsCancellationRequested && client.Connected)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                        if (bytesRead == 0) break;

                        string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        dataBuffer.Append(chunk);

                        // 提取完整 HL7 消息：以 MSH 开头，下一个 MSH 结尾
                        string allData = dataBuffer.ToString();
                        while (true)
                        {
                            int firstMsh = allData.IndexOf("MSH");
                            if (firstMsh == -1) { dataBuffer.Clear(); break; }

                            int secondMsh = allData.IndexOf("MSH", firstMsh + 3);
                            if (secondMsh == -1)
                            {
                                // 消息不完整，保留有效部分
                                if (firstMsh > 0) dataBuffer.Remove(0, firstMsh);
                                break;
                            }

                            // 取出完整消息
                            string hl7Msg = allData.Substring(firstMsh, secondMsh - firstMsh);
                            dataBuffer.Remove(0, secondMsh);
                            allData = dataBuffer.ToString();

                            // 触发事件，交由外部解析
                            OnHl7MessageReceived?.Invoke(clientIp, hl7Msg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"客户端错误: {ex.Message}");
            }
        }
        public TcpDataService(string listenIp, int port)
        {
            _listenIp = listenIp;
            _port = port;
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                _listener = new TcpListener(IPAddress.Parse(_listenIp), _port);
                _listener.Start();
                _isRunning = true;
                Console.WriteLine($"数据监听已启动: {_listenIp}:{_port}");
                _ = AcceptClientsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"无法启动数据监听 ({_listenIp}:{_port})：{ex.Message}");
            }
        }

        private async Task AcceptClientsAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();

                    // 为每个客户端创建独立的任务
                    var clientTask = HandleClientAsync(client);

                    lock (_clientsLock)
                    {
                        _activeClientTasks.Add(clientTask);
                        // 清理已完成的任务
                        _activeClientTasks.RemoveAll(t => t.IsCompleted);
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"接受客户端连接时出错: {ex.Message}");
                }
            }
        }

        //private async Task HandleClientAsync(TcpClient client)
        //{
        //    var clientEndPoint = client.Client.RemoteEndPoint.ToString();
        //    Console.WriteLine($"客户端连接: {clientEndPoint}");
        //    OnClientConnected?.Invoke(clientEndPoint);

        //    // 为每个客户端创建独立的数据缓冲区
        //    var dataBuffer = new StringBuilder();
        //    string clientIp = GetClientIp(client);

        //    lock (_bufferLock)
        //    {
        //        _deviceDataBuffers[clientIp] = dataBuffer;
        //    }

        //    try
        //    {
        //        using (client)
        //        using (var stream = client.GetStream())
        //        {
        //            var buffer = new byte[1024];

        //            while (!_cancellationTokenSource.Token.IsCancellationRequested && client.Connected)
        //            {
        //                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
        //                if (bytesRead == 0) break; // 客户端断开连接

        //                var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        //                Console.WriteLine($"从 {clientIp} 接收到数据: {data}");

        //                // 处理接收到的数据（不阻塞当前线程）
        //                _ = Task.Run(() => ProcessReceivedData(clientIp, data));
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"处理客户端数据时出错: {ex.Message}");
        //    }
        //    finally
        //    {
        //        lock (_bufferLock)
        //        {
        //            _deviceDataBuffers.Remove(clientIp);
        //        }

        //        Console.WriteLine($"客户端断开: {clientEndPoint}");
        //        OnClientDisconnected?.Invoke(clientEndPoint);
        //    }
        //}

        /// <summary>
        /// 处理接收到的数据（在独立线程中执行）
        /// </summary>
        private async Task ProcessReceivedData(string clientIp, string data)
        {
            try
            {
                StringBuilder buffer;

                // 安全地获取或创建缓冲区
                lock (_bufferLock)
                {
                    if (!_deviceDataBuffers.TryGetValue(clientIp, out buffer))
                    {
                        // 如果缓冲区不存在，创建新的
                        buffer = new StringBuilder();
                        _deviceDataBuffers[clientIp] = buffer;
                        Console.WriteLine($"为客户端 {clientIp} 创建新的缓冲区。");
                    }
                    else if (buffer == null)
                    {
                        // 如果缓冲区为null，重新创建
                        buffer = new StringBuilder();
                        _deviceDataBuffers[clientIp] = buffer;
                        Console.WriteLine($"为客户端 {clientIp} 重新创建缓冲区（原缓冲区为null）。");
                    }
                }

                // 如果buffer仍然为null，直接返回
                if (buffer == null)
                {
                    Console.WriteLine($"⚠️ 缓冲区为null，无法处理数据 from {clientIp}");
                    return;
                }

                // 添加数据到缓冲区
                lock (buffer) // 对buffer本身加锁，避免并发修改
                {
                    buffer.Append(data);
                }

                // 处理缓冲区数据
                string bufferContent;
                lock (buffer)
                {
                    bufferContent = buffer.ToString();
                }

                int jsonStart = bufferContent.IndexOf('{');
                int jsonEnd = bufferContent.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    string completeJson = bufferContent.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    // 从缓冲区移除已处理的数据（需要加锁）
                    lock (buffer)
                    {
                        // 再次检查buffer是否为null
                        if (buffer == null)
                        {
                            Console.WriteLine($"⚠️ 缓冲区在获取后变为null from {clientIp}");
                            return;
                        }

                        // 确保移除操作不会越界
                        int removeLength = jsonEnd + 1;
                        if (removeLength <= buffer.Length)
                        {
                            buffer.Remove(0, removeLength);
                        }
                        else
                        {
                            // 如果计算的长度超过了缓冲区长度，清空整个缓冲区
                            Console.WriteLine($"⚠️ 移除长度超出缓冲区长度，清空缓冲区 from {clientIp}");
                            buffer.Clear();
                        }
                    }

                    // 触发数据接收事件
                    OnDataReceived?.Invoke(completeJson);
                    Console.WriteLine($"✅ 处理完成 {clientIp} 的数据");
                }
                else
                {
                    Console.WriteLine($"⏳ 数据不完整，等待更多数据 from {clientIp}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理 {clientIp} 数据时出错: {ex.Message}");
            }
        }

        private string GetClientIp(TcpClient client)
        {
            try
            {
                var endPoint = client.Client.RemoteEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "未知IP";
            }
            catch
            {
                return "未知IP";
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();

            // 等待所有客户端任务完成
            Task.WhenAll(_activeClientTasks).Wait(TimeSpan.FromSeconds(5));
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
