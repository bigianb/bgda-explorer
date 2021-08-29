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
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using WorldExplorer.DataLoaders;
using WorldExplorer.DataLoaders.Animation;
using WorldExplorer.DataModel;
using WorldExplorer.Logging;

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

            _world = new World(engineVersion, folderPath, Path.GetFileName(_gobFile));
            _worldTreeViewModel = new WorldTreeViewModel(_world);
            OnPropertyChanged("Children");
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
        public ReadOnlyCollection<WorldTreeViewModel> Children => new ReadOnlyCollection<WorldTreeViewModel>(new[] { _worldTreeViewModel });

        private WriteableBitmap _selectedNodeImage = null;

        public WriteableBitmap SelectedNodeImage
        {
            get => _selectedNodeImage;
            set
            {
                _selectedNodeImage = value;
                OnPropertyChanged("SelectedNodeImage");
            }
        }

        private SkeletonViewModel _skeletonViewModel;

        public SkeletonViewModel TheSkeletonViewModel
        {
            get => _skeletonViewModel;
            set
            {
                _skeletonViewModel = value;
                OnPropertyChanged("TheSkeletonViewModel");
            }
        }

        private ModelViewModel _modelViewModel;

        public ModelViewModel TheModelViewModel
        {
            get => _modelViewModel;
            set
            {
                _modelViewModel = value;
                OnPropertyChanged("TheModelViewModel");
            }
        }

        LevelViewModel _levelViewModel;

        public LevelViewModel TheLevelViewModel
        {
            get => _levelViewModel;
            set
            {
                _levelViewModel = value;
                OnPropertyChanged("TheLevelViewModel");
            }
        }

        private object _selectedNode;

        public object SelectedNode
        {
            get => _selectedNode;
            set
            {
                // Clear log text
                LogText = null;

                _selectedNode = value;
                if (_selectedNode is LmpEntryTreeViewModel)
                {
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
                    OnYakChildElementSelected((YakChildTreeViewItem)_selectedNode);
                }
                else if (_selectedNode is HdrDatChildTreeViewItem)
                {
                    OnHdrDatChildElementSelected((HdrDatChildTreeViewItem)_selectedNode);
                }
                OnPropertyChanged("SelectedNode");
            }
        }

        public World World => _world;

        public MainWindow MainWindow => _window;

        private string _logText;
        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                OnPropertyChanged("LogText");
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
                        SelectedNodeImage = TexDecoder.Decode(lmpFile.FileData.AsSpan().Slice(entry.StartOffset, entry.Length));

                        _window.tabControl.SelectedIndex = 0; // Texture View
                    }
                    break;
                case ".vif":
                    {
                        var texFilename = Path.GetFileNameWithoutExtension(lmpEntry.Text) + ".tex";
                        var texEntry = lmpFile.Directory[texFilename];
                        SelectedNodeImage = TexDecoder.Decode(lmpFile.FileData.AsSpan().Slice(texEntry.StartOffset, texEntry.Length));
                        var log = new StringLogger();
                        _modelViewModel.Texture = SelectedNodeImage;
                        _modelViewModel.AnimData = null;
                        var model = new Model
                        {
                            meshList = VifDecoder.Decode(
                                log, 
                                lmpFile.FileData.AsSpan().Slice(entry.StartOffset, entry.Length),
                                SelectedNodeImage?.PixelWidth ?? 0,
                                SelectedNodeImage?.PixelHeight ?? 0)
                        };
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
                        var animData = AnmDecoder.Decode(engineVersion, lmpFile.FileData.AsSpan().Slice(entry.StartOffset, entry.Length));
                        _skeletonViewModel.AnimData = animData;
                        LogText = animData.ToString();

                        if (_modelViewModel.VifModel != null)
                        {
                            var boneCount = _modelViewModel.VifModel.CountBones();
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
                    {
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
                    }
                    _window.tabControl.SelectedIndex = 4; // Log View

                    break;
                case ".scr":
                    var script = ScrDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);
                    LogText = script.Disassemble();
                    _window.tabControl.SelectedIndex = 4; // Log View

                    break;
                case ".cut":
                    var scene = CutDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);
                    LogText = scene.Disassemble();
                    _window.tabControl.SelectedIndex = 4; // Log View

                    break;
                case ".bin":
                    {
                        var dialog = DialogDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);
                        var sb = new StringBuilder();

                        foreach (var obj in dialog)
                        {
                            sb.AppendFormat("Name: {0}\n", obj.Name);
                            sb.AppendFormat("Start offset in VA File: 0x{0:x}\n", obj.StartOffsetInVAFile);
                            sb.AppendFormat("Length: 0x{0:x}\n", obj.Length);
                            sb.Append("\n");
                        }

                        LogText = sb.ToString();
                    }
                    _window.tabControl.SelectedIndex = 4; // Log View

                    break;
            }
        }

        private void OnWorldEntrySelected(WorldFileTreeViewModel worldFileModel)
        {
            var engineVersion = App.Settings.Get("Core.EngineVersion", EngineVersion.DarkAlliance);
            var lmpFile = worldFileModel.LmpFileProperty;
            var entry = lmpFile.Directory[worldFileModel.Text];
            WorldFileDecoder decoder = engineVersion == EngineVersion.ReturnToArms || engineVersion == EngineVersion.JusticeLeagueHeroes 
                ? new WorldFileV2Decoder()
                : new WorldFileV1Decoder();
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
            SelectedNodeImage = TexDecoder.Decode(childEntry.YakFile.FileData.AsSpan().Slice(childEntry.Value.TextureOffset + childEntry.Value.VifOffset));
            var log = new StringLogger();
            _modelViewModel.Texture = SelectedNodeImage;
            _modelViewModel.AnimData = null;
            var model = new Model
            {
                meshList = VifDecoder.Decode(
                log,
                childEntry.YakFile.FileData.AsSpan().Slice(childEntry.Value.VifOffset, childEntry.Value.TextureOffset),
                SelectedNodeImage.PixelWidth,
                SelectedNodeImage.PixelHeight)
            };
            _modelViewModel.VifModel = model;

            LogText += log.ToString();

            _window.tabControl.SelectedIndex = 1; // Model View
            _window.ResetCamera();
            _window.SetViewportText(1, childEntry.Text + " of " + ((YakTreeViewItem)childEntry.Parent).Text, "");
        }

        private void OnHdrDatChildElementSelected(HdrDatChildTreeViewItem childEntry)
        {
            SelectedNodeImage = TexDecoder.Decode(childEntry.CacheFile.FileData.AsSpan().Slice(childEntry.Value.TexOffset));
            var log = new StringLogger();
            _modelViewModel.Texture = SelectedNodeImage;
            _modelViewModel.AnimData = null;
            var model = new Model
            {
                meshList = VifDecoder.Decode(
                log,
                childEntry.CacheFile.FileData.AsSpan().Slice(childEntry.Value.VifOffset, childEntry.Value.VifLength),
                SelectedNodeImage.PixelWidth,
                SelectedNodeImage.PixelHeight)
            };
            _modelViewModel.VifModel = model;

            LogText += log.ToString();

            _window.tabControl.SelectedIndex = 1; // Model View
            _window.ResetCamera();
            _window.SetViewportText(1, childEntry.Text + " of " + ((HdrDatTreeViewItem)childEntry.Parent).Text, "");
        }

        private List<AnimData> LoadFirstAnim(LmpFile lmpFile)
        {
            var animList = new List<AnimData>();
            var animEntry = lmpFile.FindFirstEntryWithSuffix(".anm");
            if (animEntry != null)
            {
                var engineVersion = App.Settings.Get("Core.EngineVersion", EngineVersion.DarkAlliance);
                animList.Add(AnmDecoder.Decode(engineVersion, lmpFile.FileData.AsSpan().Slice(animEntry.StartOffset, animEntry.Length)));
            }
            return animList;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion // INotifyPropertyChanged Members
    }
}

