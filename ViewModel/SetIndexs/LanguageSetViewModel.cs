using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WarmBox_Central_Monitoring_Station.Commands;
using WarmBox_Central_Monitoring_Station.Services;

public class LanguageSetViewModel : INotifyPropertyChanged
{
    public ObservableCollection<LanguageItem> Languages { get; } = new ObservableCollection<LanguageItem>();

    private LanguageItem _selectedLanguage;
    public LanguageItem SelectedLanguage
    {
        get => _selectedLanguage;
        set { _selectedLanguage = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }

    public LanguageSetViewModel()
    {
        // 初始化语言列表，同时指定显示名称和语言代码
        Languages.Add(new LanguageItem { DisplayName = "中文简体", Code = "zh-CN" });
        Languages.Add(new LanguageItem { DisplayName = "English", Code = "en-US" });

        // 默认选中第一项
        SelectedLanguage = Languages.FirstOrDefault();

        SaveCommand = new RelayCommand(() =>
        {
            if (SelectedLanguage != null)
            {
                LanguageService.SwitchLanguage(SelectedLanguage.Code, true);
            }
        });
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class LanguageItem
{
    public string DisplayName { get; set; }
    public string Code { get; set; }   // 新添加的语言代码
}