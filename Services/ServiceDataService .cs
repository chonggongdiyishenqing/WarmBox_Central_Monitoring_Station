using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs;
using static WarmBox_Central_Monitoring_Station.Model.Helper;
using static WarmBox_Central_Monitoring_Station.Services.IServiceDataService;
using static WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs.DeviceWeihuViewModel;
using static WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs.NetWorkSetViewModel;

namespace WarmBox_Central_Monitoring_Station.Services
{
    public class ServiceDataService : IServiceDataService
    {
        private readonly string _connectionString;

        public ServiceDataService()
        {
            string projectRoot = AppDomain.CurrentDomain.BaseDirectory;
            string solutionRoot = Directory.GetParent(projectRoot).Parent.Parent.Parent.Parent.FullName;//debug四个
            string databasePath = Path.Combine(solutionRoot, "MonitoringStationData", "MonitoringStation.db");
            _connectionString = $"Data Source={databasePath}";
        }

        public async Task InitializeDefaultUser()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var checkCommand = connection.CreateCommand();
                    checkCommand.CommandText = "SELECT COUNT(1) FROM User";
                    int userCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                    if (userCount == 0)
                    {
                        var insertCommand = connection.CreateCommand();
                        insertCommand.CommandText = @"
                            INSERT INTO User (user_account, user_password, status)
                            VALUES ($account, $password, 0)";

                        string defaultPassword = "david0527";
                        string encryptedPassword = EncryptionHelper.Encrypt(defaultPassword);

                        insertCommand.Parameters.AddWithValue("$account", "admin");
                        insertCommand.Parameters.AddWithValue("$password", encryptedPassword);

                        await insertCommand.ExecuteNonQueryAsync();
                        Console.WriteLine("✅ 默认用户已创建: admin/david0527");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 初始化用户时出错: {ex.Message}");
            }
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT user_password 
                        FROM User 
                        WHERE user_account = $username";

                    command.Parameters.AddWithValue("$username", username);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string encryptedPasswordFromDb = reader.GetString(0);
                            string decryptedPassword = EncryptionHelper.Decrypt(encryptedPasswordFromDb);
                            return decryptedPassword == password;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"验证用户时出错: {ex.Message}");
                return false;
            }
        }

        public async Task<(string username, string password)?> GetRememberedUserAsync()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT user_account, user_password 
                        FROM User 
                        WHERE status = 1 
                        LIMIT 1";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string username = reader.GetString(reader.GetOrdinal("user_account"));
                            string encryptedPassword = reader.GetString(reader.GetOrdinal("user_password"));
                            string decryptedPassword = EncryptionHelper.Decrypt(encryptedPassword);
                            return (username, decryptedPassword);
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取记住的用户时出错: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateRememberedUserAsync(string username, string password)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var clearCommand = connection.CreateCommand();
                    clearCommand.CommandText = "UPDATE User SET status = 0";
                    await clearCommand.ExecuteNonQueryAsync();

                    string encryptedPassword = EncryptionHelper.Encrypt(password);

                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = @"
                        UPDATE User 
                        SET status = 1, 
                            user_password = $password 
                        WHERE user_account = $username";

                    updateCommand.Parameters.AddWithValue("$username", username);
                    updateCommand.Parameters.AddWithValue("$password", encryptedPassword);

                    int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新记住的用户时出错: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ClearRememberedUserAsync()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE User SET status = 0";

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清除记住的用户时出错: {ex.Message}");
                return false;
            }
        }

        public async Task<List<DeviceBedInfo>> GetBoundDevicesAsync()
        {
            const string sql = @"
             SELECT 
                 d.ID,
                 d.Device_ip,
                 d.Device_Num,
                 d.Device_Name,
                 d.Device_Type AS Device_type,
                 d.Device_pingpai AS Device_xinghao,
                 COALESCE(p.Patient_Name, '未绑定') AS Patient_Name,
                 p.ID AS PatientId,
                 COALESCE(p.Patient_Chuangwei, '') AS Patient_Chuangwei,
                 COALESCE(p.Patient_Sex, 1) AS Patient_Sex,      -- 默认1男
                 COALESCE(p.Patient_GestationalAge, '0') AS Patient_GestationalAge,
                 COALESCE(p.Patient_dayold, '0') AS Patient_dayold
             FROM Device d
             LEFT JOIN patient_device pd ON d.ID = pd.deviceid
             LEFT JOIN Patient p ON pd.patientid = p.ID
             ORDER BY d.ID";

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                using var command = new SqliteCommand(sql, connection);

                var list = new List<DeviceBedInfo>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int sexValue = reader.GetInt32(reader.GetOrdinal("Patient_Sex"));
                    string sexString = sexValue == 0 ? "女" : "男";  // 默认已为1，所以不会是未知

                    string gestationalAge = reader.GetString(reader.GetOrdinal("Patient_GestationalAge"));
                    string dayOld = reader.GetString(reader.GetOrdinal("Patient_dayold"));

                    list.Add(new DeviceBedInfo
                    {
                        DeviceId = reader.GetInt32(0),
                        DeviceIp = reader.GetString(1),
                        DeviceNum = reader.GetString(2),
                        DeviceName = reader.IsDBNull(3) ? null : reader.GetString(3),
                        DeviceType = reader.IsDBNull(4) ? null : reader.GetString(4),
                        DeviceXinghao = reader.IsDBNull(5) ? null : reader.GetString(5),
                        PatientName = reader.GetString(6),
                        PatientId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                        PatientChuangwei = reader.GetString(8),
                        Sex = sexString,
                        Patient_GestationalAge = gestationalAge,
                        Patient_dayold = dayOld
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取设备列表失败: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetDeviceNumbersAsync()
        {
            try
            {
                var devices = new List<string>();
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                var sql = "SELECT Device_Num FROM Device ORDER BY ID";
                using var command = new SqliteCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    devices.Add(reader.GetString(0));
                }
                return devices;
            }
            catch (Exception ex) 
            {
                throw new Exception(ex.Message, ex);
            }
        }

        ///// <summary>
        ///// 根据设备编号获取患者信息
        ///// </summary>
        //public async Task<Patient> GetPatientByDeviceAsync(string deviceNum)
        //{
        //    try
        //    {
        //        using var connection = new SqliteConnection(_connectionString);
        //        await connection.OpenAsync();
        //        var sql = @"
        //        SELECT p.ID AS PatientId,
        //               p.Patient_Name AS Name,
        //               p.Patient_Sex AS Gender,
        //               p.Patient_BirthDay AS BirthDate,
        //               p.Patient_GestationalAge AS GestationalAge,
        //               p.Patient_Weight AS Weight,
        //               p.Patient_Height AS Height,
        //               p.Patient_dayold AS AgeDays,
        //               p.Patient_BloodType AS BloodType
        //        FROM patient_device pd
        //        INNER JOIN Device d ON pd.deviceid = d.ID
        //        INNER JOIN Patient p ON pd.patientid = p.ID
        //        WHERE d.Device_Num = @DeviceNum
        //        LIMIT 1";

        //        using var command = new SqliteCommand(sql, connection);
        //        command.Parameters.AddWithValue("@DeviceNum", deviceNum);

        //        using var reader = await command.ExecuteReaderAsync();
        //        if (await reader.ReadAsync())
        //        {
        //            var patient = new Patient
        //            {
        //                PatientId = reader.IsDBNull(reader.GetOrdinal("PatientId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PatientId")),
        //                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name")),
        //                Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? null : reader.GetInt32(reader.GetOrdinal("Gender")) == 1 ? "男" : "女",
        //                BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? (DateTime?)null : DateTime.Parse(reader.GetString(reader.GetOrdinal("BirthDate"))),
        //                GestationalAge = reader.IsDBNull(reader.GetOrdinal("GestationalAge")) ? (int?)null : int.Parse(reader.GetString(reader.GetOrdinal("GestationalAge"))),
        //                Weight = reader.IsDBNull(reader.GetOrdinal("Weight")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("Weight")),
        //                Height = reader.IsDBNull(reader.GetOrdinal("Height")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("Height")),
        //                AgeDays = reader.IsDBNull(reader.GetOrdinal("AgeDays")) ? (int?)null : int.Parse(reader.GetString(reader.GetOrdinal("AgeDays"))),
        //                BloodType = reader.IsDBNull(reader.GetOrdinal("BloodType")) ? null : reader.GetString(reader.GetOrdinal("BloodType"))
        //            };
        //            return patient;
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.Message, ex);
        //    }
        //}

        /// <summary>
        /// 根据设备编号获取患者信息（如果没有绑定病人，返回默认值）
        /// </summary>
        public async Task<Patient> GetPatientByDeviceAsync(string deviceNum)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                var sql = @"
                SELECT 
                    COALESCE(p.ID, -1) AS PatientId,
                    COALESCE(p.Patient_Name, '未绑定') AS Name,
                    COALESCE(p.Patient_Sex, 1) AS Gender,
                    p.Patient_BirthDay AS BirthDate,
                    COALESCE(p.Patient_GestationalAge, '0') AS GestationalAge,
                    COALESCE(p.Patient_Weight, 0) AS Weight,
                    COALESCE(p.Patient_Height, 0) AS Height,
                    COALESCE(p.Patient_dayold, '0') AS AgeDays,
                    COALESCE(p.Patient_BloodType, '未知') AS BloodType,
                    d.Device_ip AS DeviceIp  
                FROM Device d
                LEFT JOIN patient_device pd ON d.ID = pd.deviceid
                LEFT JOIN Patient p ON pd.patientid = p.ID
                WHERE d.Device_Num = @DeviceNum
                LIMIT 1";

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@DeviceNum", deviceNum);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var patient = new Patient
                    {
                        PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Gender = reader.GetInt32(reader.GetOrdinal("Gender")) == 1 ? "男" : "女",
                        BirthDate = reader.IsDBNull(reader.GetOrdinal("BirthDate")) ? (DateTime?)null : DateTime.Parse(reader.GetString(reader.GetOrdinal("BirthDate"))),
                        GestationalAge = int.Parse(reader.GetString(reader.GetOrdinal("GestationalAge"))),
                        Weight = reader.GetDecimal(reader.GetOrdinal("Weight")),
                        Height = reader.GetDecimal(reader.GetOrdinal("Height")),
                        AgeDays = int.Parse(reader.GetString(reader.GetOrdinal("AgeDays"))),
                        BloodType = reader.GetString(reader.GetOrdinal("BloodType")),
                        DeviceIp = reader.IsDBNull(reader.GetOrdinal("DeviceIp")) ? null : reader.GetString(reader.GetOrdinal("DeviceIp"))
                    };
                    return patient;
                }
                // 如果设备编号不存在，返回一个完全默认的患者对象（根据需求可调整）
                return new Patient
                {
                    PatientId = -1,
                    Name = "未知设备",
                    Gender = "男",
                    BirthDate = null,
                    GestationalAge = 0,
                    Weight = 0,
                    Height = 0,
                    AgeDays = 0,
                    BloodType = "未知",
                    DeviceIp = null   
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<int> GetTotalCountAsync(string tablename)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = @$"SELECT COUNT(*) FROM [{tablename}]";

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                // 记录异常信息
                throw;
            }
        }

        public async Task<List<Device>> GetDevicesByPageAsync(int pageIndex, int pageSize, string tablename)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                    SELECT 
                        ID,
                        Device_ip,
                        Device_Num,
                        Device_pingpai AS Pingpai,
                        Device_Type AS Device_type,
                        Device_keshi,
                        Device_Name
                    FROM Device
                    ORDER BY ID
                    LIMIT @PageSize OFFSET @Offset";

                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@Offset", (pageIndex - 1) * pageSize);

                        var devices = new List<Device>();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                devices.Add(new Device
                                {
                                    ID = reader["ID"]?.ToString() ?? string.Empty,
                                    Device_ip = reader["Device_ip"]?.ToString() ?? string.Empty,
                                    Device_Num = reader["Device_Num"]?.ToString() ?? string.Empty,
                                    Pingpai = reader["Pingpai"]?.ToString() ?? string.Empty,
                                    
                                    Device_type = reader["Device_type"]?.ToString() ?? string.Empty,
                                    Device_keshi = reader["Device_keshi"]?.ToString() ?? string.Empty,
                                    Device_Name = reader["Device_Name"]?.ToString() ?? string.Empty
                                    // 如果有其他字段，继续添加
                                });
                            }
                        }
                        return devices;
                    }
                }
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"数据库操作失败: {ex.Message}");
                throw new Exception($"查询设备数据失败: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取分页设备数据失败: {ex.Message}");
                throw new Exception($"获取分页设备数据失败", ex);
            }
        }

        public async Task<IEnumerable<Device>> SearchDevicesAsync(string deviceNumber, string ipAddress)
        {
            // 参数验证：至少需要一个搜索条件
            if (string.IsNullOrWhiteSpace(deviceNumber) && string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new ArgumentException("至少需要提供一个搜索条件（设备编号或IP地址）");
            }

            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = connection.CreateCommand())
                    {
                        // 构建基础查询（只查询 Device 表）
                        command.CommandText = @"
                    SELECT 
                        ID,
                        Device_ip,
                        Device_Num,
                        Device_pingpai AS Pingpai,
                        Device_Type AS Device_type,
                        Device_keshi,
                        Device_Name
                    FROM Device
                    WHERE 1=1";

                        var parameters = new List<SqliteParameter>();

                        // 添加设备编号搜索条件（精确匹配）
                        if (!string.IsNullOrWhiteSpace(deviceNumber))
                        {
                            command.CommandText += " AND Device_Num = @DeviceNumber";
                            parameters.Add(new SqliteParameter("@DeviceNumber", deviceNumber.Trim()));
                        }

                        // 添加IP地址搜索条件（精确匹配）
                        if (!string.IsNullOrWhiteSpace(ipAddress))
                        {
                            command.CommandText += " AND Device_ip = @IPAddress";
                            parameters.Add(new SqliteParameter("@IPAddress", ipAddress.Trim()));
                        }

                        // 添加排序
                        command.CommandText += " ORDER BY ID";

                        // 添加参数到命令
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }

                        var devices = new List<Device>();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var device = new Device
                                {
                                    ID = reader["ID"]?.ToString() ?? string.Empty,
                                    Device_ip = reader["Device_ip"]?.ToString() ?? string.Empty,
                                    Device_Num = reader["Device_Num"]?.ToString() ?? string.Empty,
                                    Pingpai = reader["Pingpai"]?.ToString() ?? string.Empty,
                                    Device_type = reader["Device_type"]?.ToString() ?? string.Empty,
                                    Device_keshi = reader["Device_keshi"]?.ToString() ?? string.Empty,
                                    Device_Name = reader["Device_Name"]?.ToString() ?? string.Empty
                                    // 注意：移除了原来联表查询的 Device_xinghao、Device_caiji 等字段
                                };
                                devices.Add(device);
                            }
                        }
                        return devices;
                    }
                }
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"搜索设备数据库操作失败: {ex.Message}");
                throw new Exception($"搜索设备数据失败: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索设备数据失败: {ex.Message}");
                throw new Exception($"搜索设备数据失败", ex);
            }
        }

        public async Task<ApiResponse<bool>> UpdateDeviceAsync(Device device)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
            UPDATE Device
            SET Device_ip = @ip,
                Device_Num = @num,
                Device_pingpai = @pingpai,
                Device_Type = @type,
                Device_keshi = @keshi,
                Device_Name = @name
            WHERE ID = @id";

                command.Parameters.AddWithValue("@ip", device.Device_ip);
                command.Parameters.AddWithValue("@num", device.Device_Num);
                command.Parameters.AddWithValue("@pingpai", device.Pingpai);
                command.Parameters.AddWithValue("@type", device.Device_type);
                command.Parameters.AddWithValue("@keshi", device.Device_keshi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@name", device.Device_Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@id", int.Parse(device.ID));

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    return new ApiResponse<bool> { Success = true, Message = "更新成功", Data = true };
                }
                else
                {
                    return new ApiResponse<bool> { Success = false, Message = "未找到该设备", Data = false };
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
            {
                string message = "更新失败：";
                if (ex.Message.Contains("Device_ip"))
                    message += "IP地址已存在。";
                else if (ex.Message.Contains("Device_Num"))
                    message += "设备编号已存在。";
                else
                    message += "数据违反唯一约束。";
                return new ApiResponse<bool> { Success = false, Message = message, Data = false };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"更新失败：{ex.Message}", Data = false };
            }
        }

        public async Task<ApiResponse<bool>> AddDeviceAsync(Device device)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
            INSERT INTO Device (Device_ip, Device_Num, Device_pingpai, Device_Type, Device_keshi, Device_Name)
            VALUES (@ip, @num, @pingpai, @type, @keshi, @name);
            SELECT last_insert_rowid();";

                command.Parameters.AddWithValue("@ip", device.Device_ip);
                command.Parameters.AddWithValue("@num", device.Device_Num);
                command.Parameters.AddWithValue("@pingpai", device.Pingpai);
                command.Parameters.AddWithValue("@type", device.Device_type);
                command.Parameters.AddWithValue("@keshi", device.Device_keshi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@name", device.Device_Name ?? (object)DBNull.Value);

                long newId = (long)await command.ExecuteScalarAsync();
                // 如果需要，可以将新ID赋值给 device 对象
                device.ID = newId.ToString();

                return new ApiResponse<bool> { Success = true, Message = "添加成功", Data = true };
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
            {
                string message = "添加失败：";
                if (ex.Message.Contains("Device_ip"))
                    message += "IP地址已存在。";
                else if (ex.Message.Contains("Device_Num"))
                    message += "设备编号已存在。";
                else
                    message += "数据违反唯一约束。";
                return new ApiResponse<bool> { Success = false, Message = message, Data = false };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"添加失败：{ex.Message}", Data = false };
            }
        }

        public async Task<ApiResponse<bool>> DeleteDeviceAsync(int deviceId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 使用事务
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {


                        var command = connection.CreateCommand();
                        command.CommandText = @"DELETE FROM Device WHERE ID=@ids";


                        // 添加参数到命令
                        command.Parameters.AddWithValue("ids", deviceId);

                        // 执行删除操作
                        int affectedRows = await command.ExecuteNonQueryAsync();

                        // 提交事务
                        await transaction.CommitAsync();

                        if (affectedRows > 0)
                        {
                            return ApiResponse<bool>.SuccessFunction(true, "设备删除成功");
                        }
                        else
                        {
                            return ApiResponse<bool>.Failure("删除失败", 404);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 回滚事务
                        await transaction.RollbackAsync();
                        return ApiResponse<bool>.Failure($"删除时发生错误: {ex.Message}", 500);
                    }
                }
            }
        }

        public async Task<ApiResponse<bool>> UpdateListendSet(listentset ls)
        {
            // 参数验证
            if (ls == null)
            {
                return ApiResponse<bool>.Failure("参数不能为空", 400);
            }

            if (string.IsNullOrWhiteSpace(ls.IPaddress))
            {
                return ApiResponse<bool>.Failure("IP地址不能为空", 400);
            }
            if (string.IsNullOrWhiteSpace(ls.port))
            {
                return ApiResponse<bool>.Failure("端口不能为空", 400);
            }


            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // 检查记录是否存在
                    bool recordExists;
                    using (var checkCommand = connection.CreateCommand())
                    {
                        checkCommand.CommandText = @"
                         SELECT COUNT(*) 
                         FROM ListentSet 
                         WHERE ID = 1";

                        var count = await checkCommand.ExecuteScalarAsync();
                        recordExists = Convert.ToInt32(count) > 0;
                    }

                    if (!recordExists)
                    {
                        // 如果记录不存在，可以选择创建新记录而不是返回错误
                        return await CreateDefaultRecord(connection, ls);


                    }
                    else
                    {
                        // 更新现有记录
                        using (var updateCommand = connection.CreateCommand())
                        {
                            updateCommand.CommandText = @"
                            UPDATE ListentSet 
                            SET listent_ip = @ip, listent_port = @port
                            WHERE ID = 1";

                            updateCommand.Parameters.AddWithValue("@ip", ls.IPaddress);
                            updateCommand.Parameters.AddWithValue("@port", ls.port);


                            int affectedRows = await updateCommand.ExecuteNonQueryAsync();

                            if (affectedRows == 0)
                            {
                                return ApiResponse<bool>.Failure("更新失败，记录可能不存在", 500);
                            }
                        }
                        return ApiResponse<bool>.SuccessFunction(true, "更新成功");
                    }
                }
            }
            catch (SqliteException ex)
            {
                // 记录日志
                Console.WriteLine($"数据库操作失败: {ex.Message}");
                return ApiResponse<bool>.Failure($"数据库错误: {ex.Message}", 500);
            }
            catch (Exception ex)
            {
                // 记录日志
                Console.WriteLine($"更新监听设置时发生错误: {ex.Message}");
                return ApiResponse<bool>.Failure($"系统错误: {ex.Message}", 500);
            }
        }

        private async Task<ApiResponse<bool>> CreateDefaultRecord(SqliteConnection connection, listentset ls)
        {
            try
            {
                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.CommandText = @"
                INSERT INTO ListentSet (ID, listent_ip, listent_port)
                VALUES (1, @ip, @port)";

                    insertCommand.Parameters.AddWithValue("@ip", ls.IPaddress);
                    insertCommand.Parameters.AddWithValue("@port", ls.port);


                    int affectedRows = await insertCommand.ExecuteNonQueryAsync();

                    if (affectedRows > 0)
                    {

                        return ApiResponse<bool>.SuccessFunction(true, "创建默认记录成功");
                    }
                    else
                    {
                        return ApiResponse<bool>.Failure("创建默认记录失败", 500);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建默认记录失败: {ex.Message}");
                return ApiResponse<bool>.Failure($"创建记录失败: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<listentset>> GetListendSet()
        {
            try
            {
                listentset services = null;

                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM ListentSet WHERE ID = 1";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            services = new listentset
                            {
                                // 假设 listent_ip 和 listent_port 是数据库中的列名
                                IPaddress = reader["listent_ip"]?.ToString() ?? string.Empty,
                                port = reader["listent_port"]?.ToString() ?? string.Empty
                            };
                        }
                    }
                }

                if (services != null)
                {
                    return ApiResponse<listentset>.SuccessFunction(services, "获取服务列表成功");
                }
                else
                {
                    return ApiResponse<listentset>.Failure("未找到监听服务配置", 404);
                }
            }
            catch (Exception ex)
            {
                return ApiResponse<listentset>.Failure($"获取服务列表时发生错误: {ex.Message}", 500);
            }
        }

        public async Task<List<VitalSignsRecord>> Get24HourDataAsync(string deviceIp)
        {
            // AND RecordTime >= datetime('now', '-24 hours')后面要加上
            var result = new List<VitalSignsRecord>();
            const string sql = @"
                SELECT RecordTime, HR, NIBP_SYS, NIBP_DIA, SPO2, RESP, TEMP, PI,TEMP2
                FROM VitalSignsHistory
                WHERE DeviceIP = @DeviceIP
                ORDER BY RecordTime ASC";

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqliteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@DeviceIP", deviceIp);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var record = new VitalSignsRecord
                            {
                                RecordTime = reader.GetDateTime(reader.GetOrdinal("RecordTime")),
                                HR = reader.IsDBNull(reader.GetOrdinal("HR")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("HR")),
                                NIBP_SYS = reader.IsDBNull(reader.GetOrdinal("NIBP_SYS")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("NIBP_SYS")),
                                NIBP_DIA = reader.IsDBNull(reader.GetOrdinal("NIBP_DIA")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("NIBP_DIA")),
                                SPO2 = reader.IsDBNull(reader.GetOrdinal("SPO2")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("SPO2")),
                                RESP = reader.IsDBNull(reader.GetOrdinal("RESP")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("RESP")),
                                TEMP = reader.IsDBNull(reader.GetOrdinal("TEMP")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("TEMP")),
                                TEMP2 = reader.IsDBNull(reader.GetOrdinal("TEMP2")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("TEMP2")),
                                PI = reader.IsDBNull(reader.GetOrdinal("PI")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("PI"))
                            };
                            result.Add(record);
                        }
                    }
                }
            }
            return result;
        }

        public async Task<ApiResponse<bool>> SyncPatientInfoAsync(PatientSyncInfo info)
        {
            if (info == null)
                return ApiResponse<bool>.Failure("参数不能为空", 400);

            if (string.IsNullOrWhiteSpace(info.DeviceNum))
                return ApiResponse<bool>.Failure("设备编号不能为空", 400);

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. 查找设备 ID
                        var findDeviceCmd = connection.CreateCommand();
                        findDeviceCmd.CommandText = "SELECT ID FROM Device WHERE Device_Num = @num LIMIT 1";
                        findDeviceCmd.Parameters.AddWithValue("@num", info.DeviceNum);

                        var deviceIdObj = await findDeviceCmd.ExecuteScalarAsync();
                        if (deviceIdObj == null || deviceIdObj == DBNull.Value)
                        {
                            await transaction.RollbackAsync();
                            return ApiResponse<bool>.Failure("设备不存在", 404);
                        }
                        int deviceId = Convert.ToInt32(deviceIdObj);

                        // 2. 处理患者信息（更新或新增）
                        int patientId;
                        if (info.PatientId.HasValue && info.PatientId > 0)
                        {
                            // 更新现有患者
                            var updateCmd = connection.CreateCommand();
                            updateCmd.CommandText = @"
                        UPDATE Patient
                        SET Patient_Name = @name,
                            Patient_Sex = @sex,
                            Patient_BloodType = @blood,
                            Patient_BirthDay = @birth,
                            Patient_GestationalAge = @gest,
                            Patient_Weight = @weight,
                            Patient_Height = @height,
                            Patient_dayold = @dayold,
                            Patient_Chuangwei = @chuangwei
                        WHERE ID = @id";

                            updateCmd.Parameters.AddWithValue("@name", info.Name ?? (object)DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@sex", info.Gender == "男" ? 1 : 0);
                            updateCmd.Parameters.AddWithValue("@blood", info.BloodType ?? (object)DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@birth", info.BirthDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@gest", info.GestationalAge ?? (object)DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@weight", info.Weight.HasValue ? (int)info.Weight.Value : (object)DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@height", info.Height.HasValue ? (int)info.Height.Value : (object)DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@dayold", info.AgeDays ?? (object)DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@chuangwei", info.DeviceNum);
                            updateCmd.Parameters.AddWithValue("@id", info.PatientId.Value);

                            int rows = await updateCmd.ExecuteNonQueryAsync();
                            if (rows == 0)
                            {
                                await transaction.RollbackAsync();
                                return ApiResponse<bool>.Failure("更新患者信息失败，患者可能不存在", 404);
                            }
                            patientId = info.PatientId.Value;
                        }
                        else
                        {
                            // 新增患者

                            
                            var countCmd = connection.CreateCommand();
                            countCmd.CommandText = "SELECT COUNT(*) FROM Patient";
                            var currentCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

                         
                            string newChuangwei = (currentCount + 1).ToString("D3");

                            var insertCmd = connection.CreateCommand();
                            insertCmd.CommandText = @"
        INSERT INTO Patient (Patient_Name, Patient_Sex, Patient_BloodType, Patient_BirthDay, 
                             Patient_GestationalAge, Patient_Weight, Patient_Height, 
                             Patient_dayold, Patient_Chuangwei)
        VALUES (@name, @sex, @blood, @birth, @gest, @weight, @height, @dayold, @chuangwei);
        SELECT last_insert_rowid();";

                            insertCmd.Parameters.AddWithValue("@name", info.Name ?? (object)DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@sex", info.Gender == "男" ? 1 : 0);
                            insertCmd.Parameters.AddWithValue("@blood", info.BloodType ?? (object)DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@birth", info.BirthDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@gest", info.GestationalAge ?? (object)DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@weight", info.Weight.HasValue ? (int)info.Weight.Value : (object)DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@height", info.Height.HasValue ? (int)info.Height.Value : (object)DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@dayold", info.AgeDays ?? (object)DBNull.Value);
                            // 关键改动：使用自动生成的床位数
                            insertCmd.Parameters.AddWithValue("@chuangwei", newChuangwei);

                            var newId = await insertCmd.ExecuteScalarAsync();
                            patientId = Convert.ToInt32(newId);
                        }

                        // 3. 维护 patient_device 关联表（若不存在则插入）
                        var linkCmd = connection.CreateCommand();
                        linkCmd.CommandText = @"
                    INSERT OR IGNORE INTO patient_device (patientid, deviceid)
                    VALUES (@pid, @did)";
                        linkCmd.Parameters.AddWithValue("@pid", patientId);
                        linkCmd.Parameters.AddWithValue("@did", deviceId);
                        await linkCmd.ExecuteNonQueryAsync();

                        await transaction.CommitAsync();
                        return ApiResponse<bool>.SuccessFunction(true, "同步成功");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponse<bool>.Failure($"同步失败: {ex.Message}", 500);
                    }
                }
            }
        }


        // ServiceDataService.cs
        public async Task<List<VitalSignsRecord>> GetDataAsync(string deviceIp, string parameter, DateTime date)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
            SELECT 
                ID, DeviceIP, RecordTime, 
                HR, NIBP_SYS, NIBP_DIA, SPO2, RESP, 
                TEMP, PI, TEMP2
            FROM VitalSignsHistory
            WHERE DeviceIP = @ip 
              AND RecordTime >= @start 
              AND RecordTime < @end
            ORDER BY RecordTime";

                using var command = new SqliteCommand(sql, connection);

                // 日期范围：当天 00:00:00 到次日 00:00:00
                string startStr = date.Date.ToString("yyyy-MM-dd HH:mm:ss");
                string endStr = date.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss");

                command.Parameters.AddWithValue("@ip", deviceIp);
                command.Parameters.AddWithValue("@start", startStr);
                command.Parameters.AddWithValue("@end", endStr);

                var records = new List<VitalSignsRecord>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    records.Add(new VitalSignsRecord
                    {
                        ID = reader.GetInt32(0),
                        DeviceIP = reader.GetString(1),
                        RecordTime = DateTime.Parse(reader.GetString(2)), // SQLite 存的是文本
                        HR = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                        NIBP_SYS = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                        NIBP_DIA = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                        SPO2 = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                        RESP = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                        TEMP = reader.IsDBNull(8) ? (double?)null : reader.GetInt32(8),
                        PI = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                        TEMP2 = reader.IsDBNull(10) ? (double?)null : reader.GetDouble(10)
                    });
                }

                return records;
            }
            catch (Exception ex)
            {
                // 可根据项目记录日志
                throw new Exception($"获取历史数据失败: {ex.Message}", ex);
            }
        }
    }
}
