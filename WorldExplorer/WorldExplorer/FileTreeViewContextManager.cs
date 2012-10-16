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
using WorldExplorer.DataLoaders;

namespace WorldExplorer
{
    class FileTreeViewContextManager
    {
        MainWindow _window;
        TreeView _treeView;
        ContextMenu _menu = new ContextMenu();
        
        public FileTreeViewContextManager(MainWindow window, TreeView treeView)
        {
            _window = window;
            _treeView = treeView;
            _treeView.ContextMenu = _menu;

            _treeView.ContextMenuOpening += MenuOnContextMenuOpening;


            // Setup Menu
            AddItem("Save Raw Data", SaveRawDataClicked);
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

            if (dataContext is LmpEntryTreeViewModel)
            {
                var lmpEntryItem = (LmpEntryTreeViewModel) dataContext;

                _menu.DataContext = lmpEntryItem;
                _menu.PlacementRectangle = new Rect(Mouse.GetPosition(_window), new Size(0, 0));
                _menu.IsOpen = true;
            }
            else if (dataContext is LmpTreeViewModel)
            {
                var lmpItem = (LmpTreeViewModel)dataContext;

                _menu.DataContext = lmpItem;
                _menu.PlacementRectangle = new Rect(Mouse.GetPosition(_window), new Size(0, 0));
                _menu.IsOpen = true;
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
