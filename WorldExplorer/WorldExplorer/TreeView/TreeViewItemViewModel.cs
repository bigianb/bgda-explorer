using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace WorldExplorer
{
    /// <summary>
    /// Base class for all ViewModel classes displayed by TreeViewItems.
    /// This acts as an adapter between a raw data object and a TreeViewItem.
    /// </summary>
    public class TreeViewItemViewModel : INotifyPropertyChanged
    {
        #region Data

        private static readonly TreeViewItemViewModel DummyChild = new();

        private bool _isExpanded;
        private bool _isSelected;

        #endregion // Data

        #region Constructors

        protected TreeViewItemViewModel(TreeViewItemViewModel? parent, bool lazyLoadChildren)
        {
            Parent = parent;

            Children = new ObservableCollection<TreeViewItemViewModel>();

            if (lazyLoadChildren)
            {
                Children.Add(DummyChild);
            }
        }

        // This is used to create the DummyChild instance.
        private TreeViewItemViewModel()
        {
            Children = new ObservableCollection<TreeViewItemViewModel>();
        }

        #endregion // Constructors

        #region Presentation Members

        #region Children

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public ObservableCollection<TreeViewItemViewModel> Children { get; }

        #endregion // Children

        #region HasLoadedChildren

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild => Children.Count == 1 && Children[0] == DummyChild;

        #endregion // HasLoadedChildren

        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && Parent != null)
                {
                    Parent.IsExpanded = true;
                }

                // Lazy load the child items, if necessary.
                if (HasDummyChild)
                {
                    Children.Remove(DummyChild);
                    try
                    {
                        LoadChildren();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"There was an error while trying to load the item's details.\n\nNerdy Details: {ex}",
                            "Error Loading Item", MessageBoxButton.OK);
                    }
                }
            }
        }

        #endregion // IsExpanded

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the TreeViewItem
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        #endregion // IsSelected

        #region LoadChildren

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual void LoadChildren()
        {
        }

        #endregion // LoadChildren

        #region ForceLoadChildren

        /// <summary>
        /// Forces the item to load its children without expanding.
        /// </summary>
        public void ForceLoadChildren()
        {
            if (HasDummyChild)
            {
                Children.Remove(DummyChild);
                LoadChildren();
            }
        }

        #endregion

        #region Parent

        public TreeViewItemViewModel? Parent { get; }

        #endregion // Parent

        #endregion // Presentation Members

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }
}