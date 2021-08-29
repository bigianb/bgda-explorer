
using System;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WorldExplorer.DataExporters;
using WorldExplorer.DataLoaders;
using WorldExplorer.Logging;

namespace WorldExplorer
{
    class FileTreeViewContextManager
    {
        MainWindow _window;
        TreeView _treeView;
        ContextMenu _menu = new ContextMenu();

        // Menu Items
        MenuItem _saveRawData;
        MenuItem _saveParsedVifData;
        MenuItem _logTexData;

        public FileTreeViewContextManager(MainWindow window, TreeView treeView)
        {
            _window = window;
            _treeView = treeView;
            _treeView.ContextMenu = _menu;

            _treeView.ContextMenuOpening += MenuOnContextMenuOpening;


            // Setup Menu
            _saveRawData = AddItem("Save Raw Data", SaveRawDataClicked);
            _saveParsedVifData = AddItem("Save Parsed Data", SaveParsedDataClicked);
            _logTexData = AddItem("Log .TEX Data", LogTexDataClicked);
        }

        void MenuOnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var child = GetTreeViewItemFromPoint(_treeView, Mouse.GetPosition(_treeView));
            if (child == null)
            {
                e.Handled = true;
                return;
            }
            var dataContext = child.DataContext;
            _menu.DataContext = null;

            // Set default menu item visibilty
            _saveRawData.Visibility = Visibility.Visible;
            _saveParsedVifData.Visibility = Visibility.Collapsed;
            _logTexData.Visibility = Visibility.Collapsed;

            if (dataContext is LmpEntryTreeViewModel) // files in .lmp files
            {
                var lmpEntryItem = (LmpEntryTreeViewModel)dataContext;

                if ((Path.GetExtension(lmpEntryItem.Text) ?? "").ToLower() == ".vif")
                {
                    _saveParsedVifData.Visibility = Visibility.Visible;
                }

                _menu.DataContext = lmpEntryItem;
            }
            else if (dataContext is LmpTreeViewModel) // .lmp files in .gob files
            {
                _menu.DataContext = dataContext;
            }
            else if (dataContext is WorldFileTreeViewModel) // .world files
            {
                _menu.DataContext = dataContext;

                _logTexData.Visibility = Visibility.Visible;
            }
            else if (dataContext is WorldElementTreeViewModel) // Elements of .world files
            {
                var worldElement = (WorldElementTreeViewModel)dataContext;

                _saveRawData.Visibility = Visibility.Collapsed;
                _saveParsedVifData.Visibility = Visibility.Visible;

                _menu.DataContext = worldElement;
            }
            else
            {
                e.Handled = true;
                return;
            }
        }

        #region Menu Item Click Handlers

        void SaveRawDataClicked(object sender, RoutedEventArgs e)
        {
            if (_menu.DataContext == null)
            {
                return;
            }

            if (_menu.DataContext is LmpTreeViewModel)
            {
                var lmpItem = (LmpTreeViewModel)_menu.DataContext;
                var lmpFile = lmpItem.LmpFileProperty;

                var dialog = new SaveFileDialog
                {
                    FileName = lmpItem.Text
                };

                var result = dialog.ShowDialog();
                if (result.GetValueOrDefault(false))
                {
                    using (var stream = new FileStream(dialog.FileName, FileMode.Create))
                    {
                        stream.Write(lmpFile.FileData, 0, lmpFile.FileData.Length);

                        stream.Flush();
                    }
                }
            }
            else if (_menu.DataContext is LmpEntryTreeViewModel)
            {
                var lmpEntry = (LmpEntryTreeViewModel)_menu.DataContext;
                SaveLmpEntryData(lmpEntry.LmpFileProperty, lmpEntry.Text);
            }
            else if (_menu.DataContext is WorldFileTreeViewModel)
            {
                var tvm = (WorldFileTreeViewModel)_menu.DataContext;
                SaveLmpEntryData(tvm.LmpFileProperty, tvm.Text);
            }
            else if (_menu.DataContext is WorldElementTreeViewModel)
            {
                MessageBox.Show(
                    "Saving raw world element data is not supported due to the scattered layout of the data.",
                    "Error");
                return;
            }
        }

        void SaveLmpEntryData(LmpFile lmpFile, string entryName)
        {
            var entry = lmpFile.Directory[entryName];

            var dialog = new SaveFileDialog
            {
                FileName = entryName
            };

            var result = dialog.ShowDialog();
            if (result.GetValueOrDefault(false))
            {
                using (var stream = new FileStream(dialog.FileName, FileMode.Create))
                {
                    stream.Write(lmpFile.FileData, entry.StartOffset, entry.Length);

                    stream.Flush();
                }
            }
        }

        void SaveParsedDataClicked(object sender, RoutedEventArgs e)
        {
            if (_menu.DataContext == null)
            {
                return;
            }

            if (_menu.DataContext is LmpEntryTreeViewModel)
            {
                var lmpEntry = (LmpEntryTreeViewModel)_menu.DataContext;
                var lmpFile = lmpEntry.LmpFileProperty;

                var entry = lmpFile.Directory[lmpEntry.Text];
                var texEntry = lmpFile.Directory[Path.GetFileNameWithoutExtension(lmpEntry.Text) + ".tex"];

                var tex = TexDecoder.Decode(lmpFile.FileData.AsSpan().Slice(texEntry.StartOffset, texEntry.Length));

                if ((Path.GetExtension(lmpEntry.Text) ?? "").ToLower() != ".vif")
                {
                    MessageBox.Show("Not a .vif file!", "Error");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    FileName = lmpEntry.Text + ".txt"
                };

                var result = dialog.ShowDialog();
                if (result.GetValueOrDefault(false))
                {
                    var logger = new StringLogger();
                    var chunks = VifDecoder.DecodeChunks(
                        logger,
                        lmpFile.FileData.AsSpan().Slice(entry.StartOffset, entry.Length),
                        tex.PixelWidth,
                        tex.PixelHeight);

                    VifChunkExporter.WriteChunks(dialog.FileName, chunks);
                }
            }
            else if (_menu.DataContext is WorldElementTreeViewModel)
            {
                var worldElement = (WorldElementTreeViewModel)_menu.DataContext;
                var lmpEntry = (LmpTreeViewModel)worldElement.Parent;
                var lmpFile = lmpEntry.LmpFileProperty;

                var dialog = new SaveFileDialog
                {
                    FileName = worldElement.Text + ".txt"
                };

                var result = dialog.ShowDialog();
                if (result.GetValueOrDefault(false))
                {
                    var logger = new StringLogger();
                    var chunks = VifDecoder.ReadVerts(
                        logger,
                        lmpFile.FileData.AsSpan().Slice(
                            worldElement.WorldElement.VifDataOffset, 
                            worldElement.WorldElement.VifDataOffset + worldElement.WorldElement.VifDataLength
                        )
                    );

                    VifChunkExporter.WriteChunks(dialog.FileName, chunks);
                }
            }
        }

        void LogTexDataClicked(object sender, RoutedEventArgs e)
        {
            var engineVersion = App.Settings.Get<EngineVersion>("Core.EngineVersion", EngineVersion.DarkAlliance);

            if (EngineVersion.DarkAlliance == engineVersion)
            {
                MessageBox.Show(_window, "Not supported for Dark Alliance files.", "Error", MessageBoxButton.OK);
                return;
            }

            var entries = WorldTexFile.ReadEntries(_window.ViewModel.World.WorldTex.fileData);
            var sb = new StringBuilder();

            sb.AppendLine("Debug Info For: " + _window.ViewModel.World.WorldTex.Filename);
            sb.AppendLine("");
            for (var i = 0; i < entries.Length; i++)
            {
                sb.AppendLine("Entry " + i);
                sb.AppendLine("Cell Offset: " + entries[i].CellOffset);
                sb.AppendLine("Directory Offset: " + entries[i].DirectoryOffset);
                sb.AppendLine("Size: " + entries[i].Size);

                if (i < entries.Length - 1)
                {
                    sb.AppendLine("");
                }
            }

            _window.ViewModel.LogText = sb.ToString();
            _window.tabControl.SelectedIndex = 4; // Log View
        }

        #endregion

        // Item Helpers
        private MenuItem AddItem(string text, RoutedEventHandler clickHandler)
        {
            var item = new MenuItem
            {
                Header = text
            };
            item.Click += clickHandler;

            _menu.Items.Add(item);

            return item;
        }

        // Static Methods
        private static TreeViewItem GetTreeViewItemFromPoint(TreeView treeView, Point point)
        {
            var obj = treeView.InputHitTest(point) as DependencyObject;
            while (obj != null && !(obj is TreeViewItem))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            return obj as TreeViewItem;
        }
    }
}
