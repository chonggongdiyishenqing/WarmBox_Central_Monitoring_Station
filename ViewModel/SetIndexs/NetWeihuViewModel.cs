using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WarmBox_Central_Monitoring_Station.Services;

namespace WarmBox_Central_Monitoring_Station.ViewModel.SetIndexs
{
     public class NetWeihuViewModel : INotifyPropertyChanged
    {
        private readonly IServiceDataService _serviceDataService;
        public NetWeihuViewModel(IServiceDataService serviceDataService)
        {
            _serviceDataService = serviceDataService;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
