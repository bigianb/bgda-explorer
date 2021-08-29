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

using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using WorldExplorer.DataExporters;

namespace WorldExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel _viewModel;
        FileTreeViewContextManager _fileTreeMenu;

        public MainWindow()
        {
            InitializeComponent();
            SetupViewports();

            App.LoadSettings();

            _fileTreeMenu = new FileTreeViewContextManager(this, treeView);
            _viewModel = new MainWindowViewModel(this, App.Settings.Get<string>("Files.DataPath", ""));
            DataContext = _viewModel;

            var binding = new CommandBinding(ApplicationCommands.Properties);
            binding.Executed += Properties_Executed;
            binding.CanExecute += Properties_CanExecute;
            CommandBindings.Add(binding);

            var lastLoadedFile = App.Settings.Get<string>("Files.LastLoadedFile", "");
            if (!string.IsNullOrEmpty(lastLoadedFile) && File.Exists(lastLoadedFile))
            {
                _viewModel.LoadFile(lastLoadedFile);
            }
        }

        public MainWindowViewModel ViewModel => _viewModel;

        public void ResetCamera()
        {
            switch (tabControl.SelectedIndex)
            {
                case 1:
                    modelView.viewport.SetView(new Point3D(0, -100, 0), new Vector3D(0, 100, 0), new Vector3D(0, 0, 1), 0);
                    break;
                case 2:
                    skeletonView.viewport.SetView(new Point3D(0, -100, 0), new Vector3D(0, 100, 0), new Vector3D(0, 0, 1), 0);
                    break;
                case 3:
                    var bounds = _viewModel.TheLevelViewModel.WorldBounds;
                    // hard coded for cuttown cut scene start. 187, 752, 414
                    // player 185.4157, 1401.184, 1
                    //levelView.viewport.FitView(new Vector3D(-2 / 4, 752 / 4, -400 / 4), new Vector3D(0, 0, 1));
                    levelView.viewport.ZoomExtents(bounds, 1000);
                    //levelView.viewport.SetView(new Point3D(187 / 4, 752 / 4, 414 / 4), new Vector3D(-2 / 4, 752 / 4, -400 / 4), new Vector3D(0, 0, 1), 0);
                    break;
            }
        }
        public void SetViewportText(int index, string title, string subTitle)
        {
            switch (index)
            {
                case 1:
                    if (title != null)
                    {
                        modelView.viewport.Title = title;
                    }

                    if (subTitle != null)
                    {
                        modelView.viewport.SubTitle = subTitle;
                    }

                    break;
                case 2:
                    if (title != null)
                    {
                        skeletonView.viewport.Title = title;
                    }

                    if (subTitle != null)
                    {
                        skeletonView.viewport.SubTitle = subTitle;
                    }

                    break;
                case 3:
                    if (title != null)
                    {
                        levelView.viewport.Title = title;
                    }

                    if (subTitle != null)
                    {
                        levelView.viewport.SubTitle = subTitle;
                    }

                    break;
            }
        }

        private void SetupViewports()
        {
            var viewports = new[] { modelView.viewport, skeletonView.viewport, levelView.viewport };

            foreach (var viewport in viewports)
            {
                viewport.ResetCameraGesture = null;
                viewport.ResetCameraKeyGesture = null;
                viewport.RotateGesture = new MouseGesture(MouseAction.LeftClick);
                viewport.PanGesture = new MouseGesture(MouseAction.MiddleClick);

                viewport.PreviewMouseDown += (sender, e) =>
                    {
                        if (e.ChangedButton == MouseButton.Middle && e.ClickCount > 1)
                        {
                            var view = (HelixToolkit.Wpf.HelixViewport3D)sender;
                            view.SetView(new Point3D(0, -100, 0), new Vector3D(0, 100, 0), new Vector3D(0, 0, 1), 1000);
                            e.Handled = true;
                        }
                    };
            }
        }

        public void OpenFile(string file)
        {
            _viewModel.LoadFile(file);

            var recentFiles = (App.Settings.Get("Files.RecentFiles", "") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var list = recentFiles.ToList();

            // Remove 1 from the end and anything else just in case
            if (list.Count >= 10)
            {
                list.RemoveRange(9, list.Count - 9);
            }

            // If the file is already listed remove it and add it to the top
            if (list.Contains(file))
            {
                list.Remove(file);
            }
            list.Insert(0, file);

            App.Settings["Files.RecentFiles"] = string.Join(",", list);
            App.Settings["Files.LastLoadedFile"] = file;
            App.SaveSettings();
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
            var window = new SettingsWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (window.ShowDialog() == true)
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
            var dialog = new OpenFileDialog
            {
                Multiselect = false
            };

            var result = dialog.ShowDialog();
            if (result.GetValueOrDefault(false))
            {
                OpenFile(dialog.FileName);
            }
        }
        private void MenuExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Menu_Export_Texture_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedNodeImage == null)
            {
                MessageBox.Show(this, "No texture currently loaded.", "Error", MessageBoxButton.OK);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png"
            };
            var result = dialog.ShowDialog(this);

            if (result.GetValueOrDefault(false))
            {
                using (var stream = new FileStream(dialog.FileName, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(_viewModel.SelectedNodeImage));
                    encoder.Save(stream);

                    stream.Flush();
                    stream.Close();
                }
            }
        }
        private void Menu_Export_Model_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TheModelViewModel.VifModel == null)
            {
                MessageBox.Show(this, "No model currently loaded.", "Error", MessageBoxButton.OK);
                return;
            }
            if (_viewModel.TheModelViewModel.Texture == null)
            {
                MessageBox.Show(this, "Model does not have a texture.", "Error", MessageBoxButton.OK);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "GLTF File|*.gltf|OBJ File|*.obj",
                // Select gltf by default
                FilterIndex = 1,
            };
            if (dialog.ShowDialog() != true) return;

            var ext = Path.GetExtension(dialog.FileName).ToUpperInvariant();
            IVifExporter exporter = ext switch
            {
                ".OBJ" => new VifObjExporter(),
                ".GLTF" => new VifGltfExporter(),
                _ => null
            };
            if (exporter == null)
            {
                MessageBox.Show("Unknown file format.", "Error", MessageBoxButton.OK);
                return;
            }

            exporter.SaveToFile(dialog.FileName, _viewModel.TheModelViewModel.VifModel, _viewModel.TheModelViewModel.Texture);
        }

        private void Menu_Export_PosedModel_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.TheModelViewModel.ShowExportForPosedModel();
        }

        private void MenuRecentFilesSubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuRecentFiles.Items.Clear();

            var recentFiles = (App.Settings.Get("Files.RecentFiles", "") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (recentFiles.Length > 0)
            {
                foreach (var file in recentFiles)
                {
                    var menu = new MenuItem
                    {
                        Header = file,
                        Tag = file
                    };
                    menu.Click += delegate (object o, RoutedEventArgs args)
                        {
                            var menuItem = (MenuItem)o;
                            OpenFile((string)menuItem.Tag);
                        };

                    MenuRecentFiles.Items.Add(menu);
                }
            }
            else
            {
                var menu = new MenuItem { Header = "No Recent Files", IsEnabled = false };
                MenuRecentFiles.Items.Add(menu);
            }
        }
    }
}
