using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WarmBox_Central_Monitoring_Station.ViewModel
{
    public class BedIndexViewModel : INotifyPropertyChanged
    {
        public BedViewModel Bed { get; }

        public BedIndexViewModel(BedViewModel bed)
        {
            Bed = bed;
            // 监听 Bed 的属性变化，以便通知 UI 刷新
            Bed.PropertyChanged += (s, e) =>
            {
                // 当 Bed 的任何属性变化时，通知对应属性
                OnPropertyChanged($"Bed.{e.PropertyName}");
                // 额外通知组合属性
                if (e.PropertyName == nameof(Bed.WorkMode) || e.PropertyName == nameof(Bed.WorkModePercent))
                    OnPropertyChanged(nameof(WorkModeSetDisplay));
                if (e.PropertyName == nameof(Bed.Humidity))
                    OnPropertyChanged(nameof(HumiditySetDisplay)); // 如果 HumiditySet 有独立字段则单独处理
                if (e.PropertyName == nameof(Bed.O2))
                    OnPropertyChanged(nameof(O2SetDisplay));
                if (e.PropertyName == nameof(Bed.BloodPressure))
                {
                    OnPropertyChanged(nameof(SystolicBP));
                    OnPropertyChanged(nameof(DiastolicBP));
                }
            };
        }

        // 辅助显示属性
        public string WorkModeSetDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Bed.WorkMode) || Bed.WorkModePercent == 0)
                    return "箱温模式设定：--℃"; // 默认值
                return $"{Bed.WorkMode}设定：{Bed.WorkModePercent}%";
            }
        }

        // 湿度设定（若 Bed 中有 HumiditySet 属性则绑定，否则写死）
        public string HumiditySetDisplay => "Set：60";

        // 氧浓度设定
        public string O2SetDisplay => "Set：21";

        // 血压拆分（假设 BloodPressure 格式为 "收缩压/舒张压"）
        public string SystolicBP
        {
            get
            {
                if (string.IsNullOrEmpty(Bed.BloodPressure) || Bed.BloodPressure == "--/--")
                    return "--";
                var parts = Bed.BloodPressure.Split('/');
                return parts.Length > 0 ? parts[0] : "--";
            }
        }

        public string DiastolicBP
        {
            get
            {
                if (string.IsNullOrEmpty(Bed.BloodPressure) || Bed.BloodPressure == "--/--")
                    return "--";
                var parts = Bed.BloodPressure.Split('/');
                return parts.Length > 1 ? parts[1] : "--";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
