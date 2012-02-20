using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WorldExplorer.DataLoaders;
using System.Windows.Media.Media3D;

namespace WorldExplorer
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel(string dataPath)
        {
            _selectedNodeImage = new WriteableBitmap(
                100,   // width
                100,   // height
                96,
                96,
                PixelFormats.Bgr32,
                null);

            _worlds = new ReadOnlyCollection<WorldTreeViewModel>(new []{new WorldTreeViewModel(new World(dataPath, "Cellar1"))});
        }

        private ReadOnlyCollection<WorldTreeViewModel> _worlds;

        public ReadOnlyCollection<WorldTreeViewModel> Children
        {
            get { return _worlds; }
        }

        private WriteableBitmap _selectedNodeImage=null;

        public WriteableBitmap SelectedNodeImage
        {
            get { return _selectedNodeImage; }
            set
            {
                _selectedNodeImage = value;
                this.OnPropertyChanged("SelectedNodeImage");
            }
        }

        private Model3D _selectedNodeModel;

        public Model3D SelectedNodeModel
        {
            get { return _selectedNodeModel; }
            set
            {
                _selectedNodeModel = value;
                this.OnPropertyChanged("SelectedNodeModel");
            }
        }

        private object _selectedNode;

        public object SelectedNode
        {
            get { return _selectedNode; }
            set
            {
                _selectedNode = value;
                if (_selectedNode is LmpEntryTreeViewModel) {
                    OnLmpEntrySelected((LmpEntryTreeViewModel)_selectedNode);
                }
                this.OnPropertyChanged("SelectedNode");
            }
        }

        private void OnLmpEntrySelected(LmpEntryTreeViewModel lmpEntry)
        {
            var lmpFile = lmpEntry.LmpFileProperty;
            var entry = lmpFile.Directory[lmpEntry.Text];
            if (lmpEntry.Text.EndsWith(".tex")) {              
                SelectedNodeImage = TexDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);
            } else if (lmpEntry.Text.EndsWith(".vif")) {
                SelectedNodeModel = VifDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);
            }
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }
}

