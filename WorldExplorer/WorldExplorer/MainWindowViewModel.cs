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
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using WorldExplorer.DataLoaders;
using WorldExplorer.Logging;
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

            // Create View Models
            _modelViewModel = new ModelViewModel(this);
            _skeletonViewModel = new SkeletonViewModel(this);
            _levelViewModel = new LevelViewModel(this);
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

        private SkeletonViewModel _skeletonViewModel;

        public SkeletonViewModel TheSkeletonViewModel
        {
            get { return _skeletonViewModel; }
            set
            {
                _skeletonViewModel = value;
                this.OnPropertyChanged("TheSkeletonViewModel");
            }
        }

        private ModelViewModel _modelViewModel;

        public ModelViewModel TheModelViewModel
        {
            get { return _modelViewModel; }
            set
            {
                _modelViewModel = value;
                this.OnPropertyChanged("TheModelViewModel");
            }
        }

        LevelViewModel _levelViewModel;

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
                else if (_selectedNode is YakChildTreeViewItem)
                {
                    OnYakChildElementSelected((YakChildTreeViewItem) _selectedNode);
                }
                this.OnPropertyChanged("SelectedNode");
            }
        }

        public World World
        {
            get { return _world; }
        }

        public MainWindow MainWindow
        {
            get { return _window; }
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
                        var log = new StringLogger();
                        _modelViewModel.Texture = SelectedNodeImage;
                        _modelViewModel.AnimData = null;
                        Model model = new Model();
                        model.meshList = VifDecoder.Decode(log, lmpFile.FileData, entry.StartOffset, entry.Length,
                                                           SelectedNodeImage.PixelWidth, SelectedNodeImage.PixelHeight);
                        _modelViewModel.VifModel = model;

                        /*// Load animation data
                        var animData = LoadFirstAnim(lmpFile);
                        // Make sure the animation will work with the model
                        if (animData.Count > 0 && animData[0].NumBones == model.CountBones())
                            _modelViewModel.AnimData = animData.Count == 0 ? null : animData.First();*/

                        LogText += log.ToString();

                        _window.tabControl.SelectedIndex = 1; // Model View
                        _window.ResetCamera();
                        _window.SetViewportText(1, lmpEntry.Text, "");
                    }
                    break;
                case ".anm":
                    {
                        var engineVersion = App.Settings.Get("Core.EngineVersion", EngineVersion.DarkAlliance);
                        var animData = AnmDecoder.Decode(engineVersion, lmpFile.FileData, entry.StartOffset, entry.Length);
                        _skeletonViewModel.AnimData = animData;
                        LogText = animData.ToString();

                        if (_modelViewModel.VifModel != null)
                        {
                            int boneCount = _modelViewModel.VifModel.CountBones();
                            if (boneCount != 0 && boneCount == animData.NumBones)
                            {
                                _modelViewModel.AnimData = animData;

                                // Switch tab to animation tab only if the current tab isnt the model view tab
                                if (_window.tabControl.SelectedIndex != 1) // Model View
                                {
                                    _window.tabControl.SelectedIndex = 2; // Skeleton View
                                    _window.ResetCamera();
                                }
                            }
                            else
                            {
                                // Bone count doesn't match, switch to skeleton view
                                _window.tabControl.SelectedIndex = 2; // Skeleton View
                                _window.ResetCamera();
                            }
                        }
                        else
                        {
                            _window.tabControl.SelectedIndex = 2; // Skeleton View
                            _window.ResetCamera();
                        }
                    }

                    _window.SetViewportText(2, lmpEntry.Text, ""); // Set Skeleton View Text

                    break;
                case ".ob":
                    var objects = ObDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);

                    var sb = new StringBuilder();

                    foreach (var obj in objects)
                    {
                        sb.AppendFormat("Name: {0}\n", obj.Name);
                        sb.AppendFormat("I6: {0}\n", obj.I6.ToString("X4"));
                        sb.AppendFormat("Floats: {0},{1},{2}\n", obj.Floats[0], obj.Floats[1], obj.Floats[2]);
                        if (obj.Properties != null)
                        {
                            foreach (var prop in obj.Properties)
                            {
                                sb.AppendFormat("Property: {0}\n", prop);
                            }
                        }
                        sb.Append("\n");
                    }

                    LogText = sb.ToString();
                    _window.tabControl.SelectedIndex = 4; // Log View

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
            _levelViewModel.WorldNode = worldFileModel;
            _levelViewModel.WorldData = _world.worldData;
            LogText = log.ToString();
            LogText += _world.worldData.ToString();

            _window.tabControl.SelectedIndex = 3; // Level View
            _window.ResetCamera();
            _window.SetViewportText(3, worldFileModel.Text, ""); // Set Level View Text
        }

        private void OnWorldElementSelected(WorldElementTreeViewModel worldElementModel)
        {
            SelectedNodeImage = worldElementModel.WorldElement.Texture;
            _modelViewModel.Texture = SelectedNodeImage;
            _modelViewModel.AnimData = null;
            _modelViewModel.VifModel = worldElementModel.WorldElement.model;

            _window.tabControl.SelectedIndex = 1; // Model View
            _window.ResetCamera();
            _window.SetViewportText(1, worldElementModel.Text, ""); // Set Model View Text           
        }

        private void OnYakChildElementSelected(YakChildTreeViewItem childEntry)
        {
            SelectedNodeImage = TexDecoder.Decode(childEntry.YakFile.FileData, childEntry.Value.TextureOffset + childEntry.Value.VifOffset,
                childEntry.Value.VifLength - childEntry.Value.TextureOffset);
            var log = new StringLogger();
            _modelViewModel.Texture = SelectedNodeImage;
            _modelViewModel.AnimData = null;
            Model model = new Model();
            model.meshList = VifDecoder.Decode(
                log, 
                childEntry.YakFile.FileData, 
                childEntry.Value.VifOffset,
                childEntry.Value.TextureOffset,
                SelectedNodeImage.PixelWidth,
                SelectedNodeImage.PixelHeight);
            _modelViewModel.VifModel = model;

            LogText += log.ToString();

            _window.tabControl.SelectedIndex = 1; // Model View
            _window.ResetCamera();
            _window.SetViewportText(1, childEntry.Text + " of " + ((YakTreeViewItem)childEntry.Parent).Text, "");
        }

        private List<AnimData> LoadFirstAnim(LmpFile lmpFile)
        {
            List<AnimData> animList = new List<AnimData>();
            var animEntry = lmpFile.FindFirstEntryWithSuffix(".anm");
            if (animEntry != null)
            {
                var engineVersion = App.Settings.Get("Core.EngineVersion", EngineVersion.DarkAlliance);
                animList.Add(AnmDecoder.Decode(engineVersion, lmpFile.FileData, animEntry.StartOffset, animEntry.Length));
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

