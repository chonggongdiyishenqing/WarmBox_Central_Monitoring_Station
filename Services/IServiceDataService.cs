using HandyControl.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs;
using static WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs.DeviceWeihuViewModel;
using static WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs.NetWorkSetViewModel;

namespace WarmBox_Central_Monitoring_Station.Services
{
    public interface IServiceDataService
    {
        //初始化一个登录用户
         Task InitializeDefaultUser();
         Task<bool> ValidateUserAsync(string username, string password);
         Task<(string username, string password)?> GetRememberedUserAsync();
         Task<bool> UpdateRememberedUserAsync(string username, string password);
         Task<bool> ClearRememberedUserAsync();


         Task<List<DeviceBedInfo>> GetBoundDevicesAsync();
         Task<List<string>> GetDeviceNumbersAsync();
         Task<Patient> GetPatientByDeviceAsync(string deviceNum);
         Task<List<Device>> GetDevicesByPageAsync(int pageIndex, int pageSize, string tablename);
         Task<IEnumerable<Device>> SearchDevicesAsync(string deviceNumber, string ipAddress);
         Task<int> GetTotalCountAsync(string tablename);

         Task<ApiResponse<bool>> UpdateDeviceAsync(Device device);

         Task<ApiResponse<bool>> AddDeviceAsync(Device device);

         Task<ApiResponse<bool>> DeleteDeviceAsync(int deviceId);

         Task<ApiResponse<bool>> UpdateListendSet(listentset listend);
 
         Task<ApiResponse<listentset>> GetListendSet();

        //Task<List<VitalSignsRecord>> Get24HourDataAsync(string deviceIp);

        // IServiceDataService.cs
        Task<List<VitalSignsRecord>> GetDataAsync(string deviceIp, string parameter, DateTime date);

        Task<ApiResponse<bool>> SyncPatientInfoAsync(PatientSyncInfo info);
        public class DeviceBedInfo
        {
            public int DeviceId { get; set; }
            public string DeviceIp { get; set; }
            public string DeviceNum { get; set; }          // 设备编号
            public string PatientName { get; set; }        // 绑定的患者姓名，若无则为null
            public int? PatientId { get; set; }            // 绑定的患者ID
            public string Sex { get; set; }                // 性别（女/男/未知）
            public string? DeviceName { get; set; }        // 设备名称
            public string DeviceType { get; set; }         // 设备类型
            public string DeviceXinghao { get; set; }      // 设备型号
            public string PatientChuangwei { get; set; }   // 床位号
            public string? Patient_GestationalAge { get; set; } // 胎龄（新增）
            public string? Patient_dayold { get; set; }         // 日龄（新增）
        }

    }
}
