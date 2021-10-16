using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WorldExplorer.DataExporters;
using WorldExplorer.DataLoaders;
using WorldExplorer.DataLoaders.World;
using WorldExplorer.Logging;

namespace WorldExplorer
{
    internal class FileTreeViewContextManager
    {
        private readonly MenuItem _logTexData;
        private readonly ContextMenu _menu = new();
        private readonly MenuItem _saveParsedVifData;

        // Menu Items
        private readonly MenuItem _saveRawData;
        private readonly System.Windows.Controls.TreeView _treeView;
        private readonly MainWindow _window;

        public FileTreeViewContextManager(MainWindow window, System.Windows.Controls.TreeView treeView)
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

        private void MenuOnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var child = GetTreeViewItemFromPoint(_treeView, Mouse.GetPosition(_treeView));
            if (child == null)
            {
                e.Handled = true;
                return;
            }

            var dataContext = child.DataContext;
            _menu.DataContext = null;

            // Set default menu item visibility
            _saveRawData.Visibility = Visibility.Visible;
            _saveParsedVifData.Visibility = Visibility.Collapsed;
            _logTexData.Visibility = Visibility.Collapsed;

            switch (dataContext)
            {
                // files in .lmp files
                case LmpEntryTreeViewModel lmpEntryItem:
                {
                    if (Path.GetExtension(lmpEntryItem.Label).ToUpperInvariant() == ".VIF")
                    {
                        _saveParsedVifData.Visibility = Visibility.Visible;
                    }
                    _menu.DataContext = lmpEntryItem;
                    break;
                }
                // .lmp files in .gob files
                case LmpTreeViewModel:
                    _menu.DataContext = dataContext;
                    break;
                // .world files
                case WorldFileTreeViewModel:
                    _menu.DataContext = dataContext;
                    _logTexData.Visibility = Visibility.Visible;
                    break;
                // Elements of .world files
                case WorldElementTreeViewModel model:
                {
                    var worldElement = model;
                    _saveRawData.Visibility = Visibility.Collapsed;
                    _saveParsedVifData.Visibility = Visibility.Visible;
                    _menu.DataContext = worldElement;
                    break;
                }
                default:
                    e.Handled = true;
                    break;
            }
        }

        // Item Helpers
        private MenuItem AddItem(string text, RoutedEventHandler clickHandler)
        {
            MenuItem item = new() {Header = text};
            item.Click += clickHandler;

            _menu.Items.Add(item);

            return item;
        }

        // Static Methods
        private static TreeViewItem? GetTreeViewItemFromPoint(UIElement treeView, Point point)
        {
            var obj = treeView.InputHitTest(point) as DependencyObject;
            while (obj != null && obj is not TreeViewItem)
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            return obj as TreeViewItem;
        }

        #region Menu Item Click Handlers

        private void SaveRawDataClicked(object sender, RoutedEventArgs e)
        {
            switch (_menu.DataContext)
            {
                case LmpTreeViewModel lmpItem:
                {
                    var lmpFile = lmpItem.LmpFileProperty;
                    
                    PromptToSaveData(lmpItem.Label, (saveFilePath) =>
                    {
                        using FileStream stream = new(saveFilePath, FileMode.Create);
                        stream.Write(lmpFile.FileData, 0, lmpFile.FileData.Length);
                        stream.Flush();
                    });
                    break;
                }
                case LmpEntryTreeViewModel lmpEntry:
                    SaveLmpEntryData(lmpEntry.LmpFileProperty, lmpEntry.Label);
                    break;
                case WorldFileTreeViewModel tvm:
                    SaveLmpEntryData(tvm.LmpFileProperty, tvm.Label);
                    break;
                case WorldElementTreeViewModel:
                    MessageBox.Show(
                        "Saving raw world element data is not supported due to the scattered layout of the data.",
                        "Error");
                    break;
            }
        }

        private void SaveLmpEntryData(LmpFile lmpFile, string entryName)
        {
            var entry = lmpFile.Directory[entryName];
            
            PromptToSaveData(entryName, (saveFilePath) =>
            {
                using FileStream stream = new(saveFilePath, FileMode.Create);
                stream.Write(lmpFile.FileData, entry.StartOffset, entry.Length);
                stream.Flush();
            });
        }

        private void SaveParsedDataClicked(object sender, RoutedEventArgs e)
        {
            switch (_menu.DataContext)
            {
                case LmpEntryTreeViewModel lmpEntry:
                {
                    var lmpFile = lmpEntry.LmpFileProperty;
                    var entry = lmpFile.Directory[lmpEntry.Label];

                    if (Path.GetExtension(lmpEntry.Label).ToLower() != ".vif")
                    {
                        MessageBox.Show("Not a .vif file!", "Error");
                        return;
                    }
                    
                    var fileName = lmpEntry.Label + ".txt";
                    PromptToSaveVifData(fileName, () =>
                    {
                        var texEntry =
                            lmpFile.Directory[Path.GetFileNameWithoutExtension(lmpEntry.Label) + ".tex"];
                        var texData = lmpFile.FileData.AsSpan().Slice(texEntry.StartOffset, texEntry.Length);
                        var tex = TexDecoder.Decode(texData);
                        var vifData = lmpFile.FileData.AsSpan().Slice(entry.StartOffset, entry.Length);
                        return VifDecoder.DecodeChunks(
                            NullLogger.Instance,
                            vifData,
                            tex?.PixelWidth ?? 0,
                            tex?.PixelHeight ?? 0);
                    });
                    break;
                }
                case WorldElementTreeViewModel itemModel:
                {
                    var lmpFile = (itemModel.Parent as LmpTreeViewModel)?.LmpFileProperty;
                    var element = itemModel.WorldElement;
                    if (lmpFile == null || element.DataInfo == null) return;

                    var fileName = itemModel.Label + ".txt";
                    PromptToSaveVifData(fileName, () =>
                    {
                        // TODO: Ensure this works after making VifDataOffset relative to the world file's start offset
                        var vifData = lmpFile.FileData.AsSpan().Slice(
                            element.DataInfo.VifDataOffset,
                            element.DataInfo.VifDataOffset + element.DataInfo.VifDataLength
                        );
                        return VifDecoder.ReadVerts(NullLogger.Instance, vifData);
                    });
                    break;
                }
            }
        }
        
        private void PromptToSaveData(string fileName, Action<string> saveFunc)
        {
            SaveFileDialog dialog = new() { FileName = fileName };
            if (dialog.ShowDialog() != true) return;
            saveFunc(dialog.FileName);
        }
        
        private void PromptToSaveVifData(string fileName, Func<List<VifDecoder.Chunk>> chunkFunc)
        {
            PromptToSaveData(fileName, (saveFilePath) =>
            {
                VifChunkExporter.WriteChunks(saveFilePath, chunkFunc());
            });
        }

        private void LogTexDataClicked(object sender, RoutedEventArgs e)
        {
            var engineVersion = App.Settings.Get<EngineVersion>("Core.EngineVersion");

            if (EngineVersion.DarkAlliance == engineVersion)
            {
                MessageBox.Show(_window, "Not supported for Dark Alliance files.", "Error", MessageBoxButton.OK);
                return;
            }

            var worldTex = _window.ViewModel.World?.WorldTex;

            if (worldTex == null)
            {
                MessageBox.Show(_window, "Error: Missing World Tex data.", "Error", MessageBoxButton.OK);
                return;
            }
            
            var entries = WorldTexFile.ReadEntries(worldTex.FileData);
            StringBuilder sb = new();

            sb.AppendLine($"Debug Info For: {worldTex.FileName}");
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
    }
}