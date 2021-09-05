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

using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        private FileTreeViewContextManager _fileTreeMenu;

        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            SetupViewports();

            App.LoadSettings();

            _fileTreeMenu = new FileTreeViewContextManager(this, treeView);
            ViewModel = new MainWindowViewModel(this, App.Settings.Get("Files.DataPath", "") ?? "");
            DataContext = ViewModel;

            CommandBinding binding = new(ApplicationCommands.Properties);
            binding.Executed += Properties_Executed;
            binding.CanExecute += Properties_CanExecute;
            CommandBindings.Add(binding);

            var lastLoadedFile = App.Settings.Get("Files.LastLoadedFile", "");
            if (!string.IsNullOrEmpty(lastLoadedFile) && File.Exists(lastLoadedFile))
            {
                ViewModel.LoadFile(lastLoadedFile);
            }
        }

        public void ResetCamera()
        {
            switch (tabControl.SelectedIndex)
            {
                case 1:
                    modelView.viewport.SetView(new Point3D(0, -100, 0), new Vector3D(0, 100, 0), new Vector3D(0, 0, 1));
                    break;
                case 2:
                    skeletonView.viewport.SetView(new Point3D(0, -100, 0), new Vector3D(0, 100, 0),
                        new Vector3D(0, 0, 1));
                    break;
                case 3:
                    // Attempt to get the whole world in view
                    var bounds = ViewModel.TheLevelViewModel.WorldBounds;
                    levelView.viewport.ZoomExtents(bounds, 1000);
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
            HelixViewport3D[] viewports = {modelView.viewport, skeletonView.viewport, levelView.viewport};

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
                        var view = (HelixViewport3D)sender;
                        view.SetView(new Point3D(0, -100, 0), new Vector3D(0, 100, 0), new Vector3D(0, 0, 1), 1000);
                        e.Handled = true;
                    }
                };
            }
        }

        public void OpenFile(string file)
        {
            ViewModel.LoadFile(file);

            var recentFiles =
                (App.Settings.Get("Files.RecentFiles", "") ?? "").Split(new[] {','},
                    StringSplitOptions.RemoveEmptyEntries);
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
            SettingsWindow window = new() {Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner};
            if (window.ShowDialog() == true)
                // User pressed save, so we should re-init things.
            {
                ViewModel.SettingsChanged();
            }
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.SelectedNode = e.NewValue;
        }

        private void MenuOpenFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new() {Multiselect = false};

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
            if (ViewModel.SelectedNodeImage == null)
            {
                MessageBox.Show(this, "No texture currently loaded.", "Error", MessageBoxButton.OK);
                return;
            }

            SaveFileDialog dialog = new() {Filter = "PNG Image|*.png"};
            var result = dialog.ShowDialog(this);

            if (result.GetValueOrDefault(false))
            {
                using (FileStream stream = new(dialog.FileName, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new();
                    encoder.Frames.Add(BitmapFrame.Create(ViewModel.SelectedNodeImage));
                    encoder.Save(stream);

                    stream.Flush();
                    stream.Close();
                }
            }
        }

        private void Menu_Export_Model_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.TheModelViewModel.VifModel == null)
            {
                MessageBox.Show(this, "No model currently loaded.", "Error", MessageBoxButton.OK);
                return;
            }

            if (ViewModel.TheModelViewModel.Texture == null)
            {
                MessageBox.Show(this, "Model does not have a texture.", "Error", MessageBoxButton.OK);
                return;
            }

            SaveFileDialog dialog = new()
            {
                Filter = "GLTF File|*.gltf|OBJ File|*.obj",
                // Select gltf by default
                FilterIndex = 1,
                FileName = "some-model.gltf"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var ext = Path.GetExtension(dialog.FileName).ToUpperInvariant();
            IVifExporter? exporter = ext switch
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

            exporter.SaveToFile(dialog.FileName, ViewModel.TheModelViewModel.VifModel,
                ViewModel.TheModelViewModel.Texture);
        }

        private void Menu_Export_PosedModel_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TheModelViewModel.ShowExportForPosedModel();
        }

        private void MenuRecentFilesSubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuRecentFiles.Items.Clear();

            var recentFiles =
                (App.Settings.Get("Files.RecentFiles", "") ?? "").Split(new[] {','},
                    StringSplitOptions.RemoveEmptyEntries);

            if (recentFiles.Length > 0)
            {
                foreach (var file in recentFiles)
                {
                    MenuItem menu = new() {Header = file, Tag = file};
                    menu.Click += delegate(object o, RoutedEventArgs args)
                    {
                        var menuItem = (MenuItem)o;
                        OpenFile((string)menuItem.Tag);
                    };

                    MenuRecentFiles.Items.Add(menu);
                }
            }
            else
            {
                MenuItem menu = new() {Header = "No Recent Files", IsEnabled = false};
                MenuRecentFiles.Items.Add(menu);
            }
        }
    }
}