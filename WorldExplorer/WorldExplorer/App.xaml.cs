using System;
using System.IO;
using System.Windows;
using WorldExplorer.Tools;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static string SettingsFileName = "settings.ini";
        public static Section Settings;

        public static void LoadSettings()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(ResourceAssembly.Location);
            Settings = File.Exists(SettingsFileName) ? SettingsIO.Ini.ParseFile(SettingsFileName) : new Section();
        }

        public static void SaveSettings()
        {
            SettingsIO.Ini.WriteFile(SettingsFileName, Settings);
        }
    }
}
