using System;
using System.Collections.Generic;
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

            gobTextblock.Text = Properties.Settings.Default.GobFile;
            dataPathTextblock.Text = Properties.Settings.Default.DataPath;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.GobFile = gobTextblock.Text;
            Properties.Settings.Default.DataPath = dataPathTextblock.Text;
            Properties.Settings.Default.Save();
            DialogResult = true;
            Close();
        }
    }
}
