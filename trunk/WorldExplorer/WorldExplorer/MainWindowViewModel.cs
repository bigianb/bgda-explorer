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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WorldExplorer.DataLoaders;
using System.Windows.Media.Media3D;
using WorldExplorer.Logging;
using WorldExplorer.Tools3D;
using WorldExplorer.DataModel;

namespace WorldExplorer
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        string _gobFile;
        string _dataPath;
        MainWindow _window;

        public MainWindowViewModel(MainWindow window, string dataPath)
        {
            _window = window;
            _dataPath = dataPath;
            /*_selectedNodeImage = new WriteableBitmap(
                100,   // width
                100,   // height
                96,
                96,
                PixelFormats.Bgr32,
                null);*/
        }

        public void LoadFile(string file)
        {
            // Clear log text
            LogText = null;

            var folderPath = Path.GetDirectoryName(file);
            var engineVersion = App.Settings.Get<EngineVersion>("Core.EngineVersion", EngineVersion.DarkAlliance);
            _gobFile = file;

            _world = new World(engineVersion, folderPath,  Path.GetFileName(_gobFile));
            _worldTreeViewModel = new WorldTreeViewModel(_world);
            this.OnPropertyChanged("Children");
        }

        public void SettingsChanged()
        {
            _dataPath = App.Settings.Get<string>("Files.DataPath", "");

            if (_gobFile != null)
            {
                // Reload file with new settings
                LoadFile(_gobFile);
            }
        }

        private World _world;
        private WorldTreeViewModel _worldTreeViewModel;

        // This is what the tree view binds to.
        public ReadOnlyCollection<WorldTreeViewModel> Children
        {
            get { return new ReadOnlyCollection<WorldTreeViewModel>(new[] { _worldTreeViewModel }); }
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

        private SkeletonViewModel _skeletonViewModel = new SkeletonViewModel();

        public SkeletonViewModel TheSkeletonViewModel
        {
            get { return _skeletonViewModel; }
            set
            {
                _skeletonViewModel = value;
                this.OnPropertyChanged("TheSkeletonViewModel");
            }
        }

        private ModelViewModel _modelViewModel = new ModelViewModel();

        public ModelViewModel TheModelViewModel
        {
            get { return _modelViewModel; }
            set
            {
                _modelViewModel = value;
                this.OnPropertyChanged("TheModelViewModel");
            }
        }

        private LevelViewModel _levelViewModel = new LevelViewModel();

        public LevelViewModel TheLevelViewModel
        {
            get { return _levelViewModel; }
            set
            {
                _levelViewModel = value;
                this.OnPropertyChanged("TheLevelViewModel");
            }
        }

        private object _selectedNode;

        public object SelectedNode
        {
            get { return _selectedNode; }
            set
            {
                // Clear log text
                LogText = null;

                _selectedNode = value;
                if (_selectedNode is LmpEntryTreeViewModel) {
                    OnLmpEntrySelected((LmpEntryTreeViewModel)_selectedNode);
                }
                else if (_selectedNode is WorldFileTreeViewModel)
                {
                    OnWorldEntrySelected((WorldFileTreeViewModel)_selectedNode);
                }
                else if (_selectedNode is WorldElementTreeViewModel)
                {
                    OnWorldElementSelected((WorldElementTreeViewModel)_selectedNode);
                }
                this.OnPropertyChanged("SelectedNode");
            }
        }

        public string _logText;
        public String LogText
        {
            get { return _logText; }
            set
            {
                _logText = value;
                this.OnPropertyChanged("LogText");
            }
        }

        private void OnLmpEntrySelected(LmpEntryTreeViewModel lmpEntry)
        {
            var lmpFile = lmpEntry.LmpFileProperty;
            var entry = lmpFile.Directory[lmpEntry.Text];

            var ext = (Path.GetExtension(lmpEntry.Text) ?? "").ToLower();

            switch (ext)
            {
                case ".tex":
                    {
                        SelectedNodeImage = TexDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);

                        _window.tabControl.SelectedIndex = 0; // Texture View
                    }
                    break;
                case ".vif":
                    {
                        string texFilename = Path.GetFileNameWithoutExtension(lmpEntry.Text) + ".tex";
                        var texEntry = lmpFile.Directory[texFilename];
                        SelectedNodeImage = TexDecoder.Decode(lmpFile.FileData, texEntry.StartOffset, texEntry.Length);
                        var animData = LoadFirstAnim(lmpFile);
                        var log = new StringLogger();
                        _modelViewModel.Texture = SelectedNodeImage;
                        _modelViewModel.AnimData = null;
                        Model model = new Model();
                        model.meshList = VifDecoder.Decode(log, lmpFile.FileData, entry.StartOffset, entry.Length,
                                                           SelectedNodeImage.PixelWidth, SelectedNodeImage.PixelHeight);
                        _modelViewModel.VifModel = model;
                        _modelViewModel.AnimData = animData.Count == 0 ? null : animData.First();
                        LogText += log.ToString();

                        _window.tabControl.SelectedIndex = 1; // Model View
                    }
                    break;
                case ".anm":
                    {
                        var animData = AnmDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);
                        _skeletonViewModel.AnimData = animData;
                        LogText = animData.ToString();

                        _window.tabControl.SelectedIndex = 2; // Animation View
                    }
                    break;
            }
        }

        private void OnWorldEntrySelected(WorldFileTreeViewModel worldFileModel)
        {
            var engineVersion = App.Settings.Get<EngineVersion>("Core.EngineVersion", EngineVersion.DarkAlliance);
            var lmpFile = worldFileModel.LmpFileProperty;
            var entry = lmpFile.Directory[worldFileModel.Text];
            WorldFileDecoder decoder = new WorldFileDecoder();
            var log = new StringLogger();
            _world.worldData = decoder.Decode(engineVersion, _worldTreeViewModel.World().WorldTex, log, lmpFile.FileData, entry.StartOffset, entry.Length);
            worldFileModel.ReloadChildren();
            _levelViewModel.WorldData = _world.worldData;
            LogText = log.ToString();
            LogText += _world.worldData.ToString();

            _window.tabControl.SelectedIndex = 3; // World View
        }

        private void OnWorldElementSelected(WorldElementTreeViewModel worldElementModel)
        {
            SelectedNodeImage = worldElementModel.WorldElement.Texture;
            _modelViewModel.Texture = SelectedNodeImage;
            _modelViewModel.AnimData = null;
            _modelViewModel.VifModel = worldElementModel.WorldElement.model;

            // If there is a texture that means there's a model
            if (SelectedNodeImage != null)
            {
                _window.tabControl.SelectedIndex = 1; // Model View
            }
        }

        private List<AnimData> LoadFirstAnim(LmpFile lmpFile)
        {
            List<AnimData> animList = new List<AnimData>();
            var animEntry = lmpFile.FindFirstEntryWithSuffix(".anm");
            if (animEntry != null)
            {
                animList.Add(AnmDecoder.Decode(lmpFile.FileData, animEntry.StartOffset, animEntry.Length));
            }
            return animList;
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

