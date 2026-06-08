using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace WarmBox_Central_Monitoring_Station.Services
{
    public static class LanguageService
    {
        private const string ConfigFileName = "language_config.json";
        private static readonly string ConfigFilePath;
        public static event Action<string> LanguageChanged;

        static LanguageService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            ConfigFilePath = Path.Combine(appData, "WarmBox", ConfigFileName);
        }

        public static void SwitchLanguage(string cultureName, bool showPrompt = false)
        {
            // 1. 保存用户选择
            SaveCurrentLanguage(cultureName);
            
            // 2. 加载新的语言资源字典
            var newDict = new ResourceDictionary();
            string path = $"Languages/{cultureName}.xaml";
            try
            {
                newDict.Source = new Uri(path, UriKind.Relative);
            }
            catch (Exception)
            {
                // 如果加载失败（例如文件被删除），退回默认中文
                if (cultureName != "zh-CN")
                {
                    SwitchLanguage("zh-CN", showPrompt);
                    if (showPrompt)
                    {
                        MessageBox.Show("语言文件缺失，已恢复为默认中文。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                return;
            }

            // 3. 替换应用程序中的语言字典
            var appDict = Application.Current.Resources.MergedDictionaries;
            var oldDict = appDict.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Languages/"));
            if (oldDict != null)
                appDict.Remove(oldDict);
            appDict.Add(newDict);

            // 4. 显示语言切换成功提示（仅手动切换时弹出）
            if (showPrompt)
            {
                string message = Application.Current.TryFindResource("LanguageChangedMessage") as string
                                 ?? "语言切换成功！";
                string title = cultureName == "en-US" ? "Info" : "提示";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            LanguageChanged?.Invoke(cultureName);
        }

        public static string GetCurrentLanguage()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<LanguageConfig>(json);
                    return config?.CultureName ?? "zh-CN";
                }
            }
            catch { /* 忽略异常，使用默认值 */ }
            return "zh-CN";
        }

        private static void SaveCurrentLanguage(string cultureName)
        {
            var config = new LanguageConfig { CultureName = cultureName };
            string dir = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config));
        }

        private class LanguageConfig
        {
            public string CultureName { get; set; }
        }
    }
}