using System;
using System.IO;
using WorldExplorer.Tools;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static readonly string SettingsFileName = "settings.ini";
        public static Section Settings = new();

        public static void LoadSettings()
        {
            var appDirectory = Path.GetDirectoryName(ResourceAssembly.Location);
            if (appDirectory != null)
                Environment.CurrentDirectory = appDirectory;
            Settings = File.Exists(SettingsFileName) 
                ? SettingsIO.Ini.ParseFile(SettingsFileName) 
                : new Section();
        }

        public static void SaveSettings()
        {
            SettingsIO.Ini.WriteFile(SettingsFileName, Settings);
        }
    }
}