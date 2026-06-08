using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarmBox_Central_Monitoring_Station.Model;

namespace WarmBox_Central_Monitoring_Station.Services
{
    public class AlarmLogService
    {
        private const int MaxLogCount = 2000;
        public static readonly AlarmLogService Instance = new AlarmLogService();

        public ObservableCollection<AlarmLogItem> AlarmLogs { get; } = new ObservableCollection<AlarmLogItem>();

        private AlarmLogService() { }

        public void AddLog(AlarmLogItem item)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (AlarmLogs.Count >= MaxLogCount)
                    AlarmLogs.Clear();
                AlarmLogs.Add(item);
            });
        }

        public void ClearAll()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => AlarmLogs.Clear());
        }
    }
}
