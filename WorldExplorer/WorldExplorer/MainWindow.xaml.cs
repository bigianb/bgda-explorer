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

            _viewModel = new MainWindowViewModel(Properties.Settings.Default.DataPath);
            DataContext = _viewModel;

            var binding = new CommandBinding(ApplicationCommands.Properties);
            binding.Executed += Properties_Executed;
            binding.CanExecute += Properties_CanExecute;
            this.CommandBindings.Add(binding);
        }

        private void Properties_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Properties_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new SettingsWindow();
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

            if (dialog.ShowDialog(this).Value)
            {
                _viewModel.LoadFile(dialog.FileName);
                //viewModel.SettingsChanged();
            }
        }

    }
}
