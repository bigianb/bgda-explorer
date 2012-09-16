using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WorldExplorer.DataLoaders;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            // Add engine versions to combo box
            engineVersionBox.Items.Add(new ComboBoxItem() { Content = "Dark Alliance 1", DataContext = EngineVersion.DarkAlliance });
            engineVersionBox.Items.Add(new ComboBoxItem() { Content = "Champions: Return To Arms", DataContext = EngineVersion.ReturnToArms });

            // Select the correct item
            foreach (ComboBoxItem item in engineVersionBox.Items)
            {
                if (item.DataContext is EngineVersion && (EngineVersion)item.DataContext == Properties.Settings.Default.EngineVersion)
                {
                    engineVersionBox.SelectedItem = item;
                    break;
                }
            }
            dataPathTextblock.Text = Properties.Settings.Default.DataPath;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DataPath = dataPathTextblock.Text;
            Properties.Settings.Default.EngineVersion = GetVersionFromBox();

            Properties.Settings.Default.Save();
            DialogResult = true;
            Close();
        }

        private EngineVersion GetVersionFromBox()
        {
            foreach (ComboBoxItem item in engineVersionBox.Items)
            {
                if (item.IsSelected)
                {
                    if (item.DataContext is EngineVersion)
                    {
                        return (EngineVersion) item.DataContext;
                    }
                }
            }

            // Return default version
            return EngineVersion.DarkAlliance;
        }
    }
}
