using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Model;
using WarmBox_Central_Monitoring_Station.Services;

namespace WarmBox_Central_Monitoring_Station.ViewModel
{
    public class SetBaojinListViewModel : INotifyPropertyChanged
    {
        private readonly AlarmLogService _alarmLogService;

        public ObservableCollection<AlarmLogItem> AlarmItems { get; private set; } = new ObservableCollection<AlarmLogItem>();

        private int _pageSize = 20;
        public int PageSize { get => _pageSize; set { _pageSize = value; OnPropertyChanged(); UpdatePaging(); } }
        public List<int> PageSizes { get; } = new() { 10, 20, 50, 100 };

        private int _currentPage = 1;
        public int CurrentPage { get => _currentPage; set { _currentPage = value; OnPropertyChanged(); UpdatePaging(); } }

        public int TotalPages => (int)Math.Ceiling((double)(_alarmLogService?.AlarmLogs.Count ?? 0) / _pageSize);

        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand ClearAllCommand { get; }

        public SetBaojinListViewModel()
        {
            _alarmLogService = AlarmLogService.Instance;

            if (_alarmLogService != null)
                _alarmLogService.AlarmLogs.CollectionChanged += (_, _) => UpdatePaging();

            FirstPageCommand = new RelayCommand(() => CurrentPage = 1);
            PreviousPageCommand = new RelayCommand(() => { if (CurrentPage > 1) CurrentPage--; });
            NextPageCommand = new RelayCommand(() => { if (CurrentPage < TotalPages) CurrentPage++; });
            LastPageCommand = new RelayCommand(() => CurrentPage = TotalPages);
            ClearAllCommand = new RelayCommand(() => _alarmLogService?.ClearAll());

            UpdatePaging();
        }

        private void UpdatePaging()
        {
            if (_alarmLogService == null) return;
            var source = _alarmLogService.AlarmLogs;
            var items = source.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            AlarmItems = new ObservableCollection<AlarmLogItem>(items);
            OnPropertyChanged(nameof(AlarmItems));
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(TotalPages));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
