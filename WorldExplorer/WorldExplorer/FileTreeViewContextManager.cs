using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
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
        
        public FileTreeViewContextManager(MainWindow window, TreeView treeView)
        {
            _window = window;
            _treeView = treeView;
            _treeView.ContextMenu = _menu;

            _treeView.ContextMenuOpening += MenuOnContextMenuOpening;


            // Setup Menu
            _saveRawData = AddItem("Save Raw Data", SaveRawDataClicked);
            _saveParsedVifData = AddItem("Save Parsed Data", SaveParsedDataClicked);
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

            if (dataContext is LmpEntryTreeViewModel) // files in .lmp files
            {
                var lmpEntryItem = (LmpEntryTreeViewModel) dataContext;

                if ((Path.GetExtension(lmpEntryItem.Text) ?? "").ToLower() == ".vif")
                {
                    _saveParsedVifData.Visibility = Visibility.Visible;
                }

                _menu.DataContext = lmpEntryItem;
            }
            else if (dataContext is LmpTreeViewModel) // .lmp files in .gob files
            {
                var lmpItem = (LmpTreeViewModel)dataContext;

                _menu.DataContext = lmpItem;
            }
            else if (dataContext is WorldElementTreeViewModel)
            {
                var worldElement = (WorldElementTreeViewModel) dataContext;

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
                return;

            if (_menu.DataContext is LmpTreeViewModel)
            {
                var lmpItem = (LmpTreeViewModel) _menu.DataContext;
                var lmpFile = lmpItem.LmpFileProperty;

                var dialog = new SaveFileDialog();
                dialog.FileName = lmpItem.Text;

                bool? result = dialog.ShowDialog();
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
                var lmpEntry = (LmpEntryTreeViewModel) _menu.DataContext;
                var lmpFile = lmpEntry.LmpFileProperty;
                var entry = lmpFile.Directory[lmpEntry.Text];

                var dialog = new SaveFileDialog();
                dialog.FileName = lmpEntry.Text;

                bool? result = dialog.ShowDialog();
                if (result.GetValueOrDefault(false))
                {
                    using (var stream = new FileStream(dialog.FileName, FileMode.Create))
                    {
                        stream.Write(lmpFile.FileData, entry.StartOffset, entry.Length);

                        stream.Flush();
                    }
                }
            }
            else if (_menu.DataContext is WorldElementTreeViewModel)
            {
                MessageBox.Show(
                    "Saving raw world element data is not supported due to the scattered layout of the data.",
                    "Error");
                return;
            }
        }

        void SaveParsedDataClicked(object sender, RoutedEventArgs e)
        {
            if (_menu.DataContext == null)
                return;

            if (_menu.DataContext is LmpEntryTreeViewModel)
            {
                var lmpEntry = (LmpEntryTreeViewModel)_menu.DataContext;
                var lmpFile = lmpEntry.LmpFileProperty;

                var entry = lmpFile.Directory[lmpEntry.Text];
                var texEntry = lmpFile.Directory[Path.GetFileNameWithoutExtension(lmpEntry.Text)+".tex"];

                var tex = TexDecoder.Decode(lmpFile.FileData, texEntry.StartOffset, texEntry.Length);

                if ((Path.GetExtension(lmpEntry.Text) ?? "").ToLower() != ".vif")
                {
                    MessageBox.Show("Not a .vif file!", "Error");
                    return;
                }

                var dialog = new SaveFileDialog();
                dialog.FileName = lmpEntry.Text+".txt";

                bool? result = dialog.ShowDialog();
                if (result.GetValueOrDefault(false))
                {
                    var exporter = new VifExporter();

                    var logger = new StringLogger();
                    var chunks = VifDecoder.DecodeChunks(
                        logger, 
                        lmpFile.FileData, 
                        entry.StartOffset, 
                        entry.Length, 
                        tex.PixelWidth,
                        tex.PixelHeight);

                    exporter.WriteChunks(dialog.FileName, chunks);
                }
            }
            else if (_menu.DataContext is WorldElementTreeViewModel)
            {
                var worldElement = (WorldElementTreeViewModel)_menu.DataContext;
                var lmpEntry = (LmpTreeViewModel)worldElement.Parent;
                var lmpFile = lmpEntry.LmpFileProperty;

                var dialog = new SaveFileDialog();
                dialog.FileName = worldElement.Text + ".txt";

                bool? result = dialog.ShowDialog();
                if (result.GetValueOrDefault(false))
                {
                    var exporter = new VifExporter();

                    var logger = new StringLogger();
                    var chunks = VifDecoder.ReadVerts(
                        logger,
                        lmpFile.FileData,
                        worldElement.WorldElement.VifDataOffset,
                        worldElement.WorldElement.VifDataOffset + worldElement.WorldElement.VifDataLength);

                    exporter.WriteChunks(dialog.FileName, chunks);
                }
            }
        }

        #endregion

        // Item Helpers
        private MenuItem AddItem(string text, RoutedEventHandler clickHandler)
        {
            var item = new MenuItem();
            item.Header = text;
            item.Click += clickHandler;

            _menu.Items.Add(item);

            return item;
        }

        // Static Methods
        private static TreeViewItem GetTreeViewItemFromPoint(TreeView treeView, Point point)
        {
            DependencyObject obj = treeView.InputHitTest(point) as DependencyObject;
            while (obj != null && !(obj is TreeViewItem))
                obj = VisualTreeHelper.GetParent(obj);
            return obj as TreeViewItem;
        }
    }
}
