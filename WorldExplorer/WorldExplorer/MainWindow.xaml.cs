/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using WorldExplorer.Tools3D;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            App.LoadSettings();

            _viewModel = new MainWindowViewModel(this, App.Settings.Get<string>("Files.DataPath", ""));
            DataContext = _viewModel;

            var binding = new CommandBinding(ApplicationCommands.Properties);
            binding.Executed += Properties_Executed;
            binding.CanExecute += Properties_CanExecute;
            this.CommandBindings.Add(binding);

            var lastLoadedFile = App.Settings.Get<string>("Files.LastLoadedFile", "");
            if (!string.IsNullOrEmpty(lastLoadedFile))
            {
                _viewModel.LoadFile(lastLoadedFile);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            App.SaveSettings();
        }

        private void Properties_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Properties_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new SettingsWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            bool? result = window.ShowDialog();
            if (result.GetValueOrDefault(false))
            {
                // User pressed save, so we should re-init things.
                _viewModel.SettingsChanged();
            }
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _viewModel.SelectedNode = e.NewValue;
        }

        private void MenuOpenFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = false;

            bool? result = dialog.ShowDialog();
            if (result.GetValueOrDefault(false))
            {
                _viewModel.LoadFile(dialog.FileName);

                App.Settings["Files.LastLoadedFile"] = dialog.FileName;
                App.SaveSettings();
            }
        }
        private void MenuExitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Menu_Export_Texture_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedNodeImage == null)
            {
                MessageBox.Show(this, "No texture currently loaded.", "Error", MessageBoxButton.OK);
                return;
            }

            var dialog = new SaveFileDialog();
            dialog.Filter = "PNG Image|*.png";
            var result = dialog.ShowDialog(this);

            if (result.GetValueOrDefault(false))
            {
                using (FileStream stream5 = new FileStream(dialog.FileName, FileMode.Create))
                {
                    var encoder5 = new PngBitmapEncoder();
                    encoder5.Frames.Add(BitmapFrame.Create(_viewModel.SelectedNodeImage));
                    encoder5.Save(stream5);
                    stream5.Close();
                }
            }
        }
    }
}
