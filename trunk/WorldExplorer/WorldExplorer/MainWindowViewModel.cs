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
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WorldExplorer.DataLoaders;
using System.Windows.Media.Media3D;
using WorldExplorer.Logging;
using WorldExplorer.Tools3D;

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

        private Transform3D _cameraTransform;

        public Transform3D CameraTransform
        {
            get { return _cameraTransform; }
            set
            {
                _cameraTransform = value;
                _selectedNodeCamera.Transform = _cameraTransform;
                this.OnPropertyChanged("CameraTransform");
            }
        }

        private Camera _selectedNodeCamera = new OrthographicCamera{Position=new Point3D(0, 10, -10), LookDirection=new Vector3D(0,-1,1)};

        public Camera SelectedNodeCamera
        {
            get { return _selectedNodeCamera; }
            set
            {
                _selectedNodeCamera = value;
                this.OnPropertyChanged("SelectedNodeCamera");
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

        private void UpdateCamera(Model3D model, OrthographicCamera oCam)
        {
            var bounds = model.Bounds;
            //Point3D centroid = new Point3D(bounds.X + bounds.SizeX / 2.0, bounds.Y + bounds.SizeY / 2.0, bounds.Z + bounds.SizeZ / 2.0);
            Point3D centroid = new Point3D(0, 0, 0);
            double radius = Math.Sqrt(bounds.SizeX * bounds.SizeX + bounds.SizeY * bounds.SizeY + bounds.SizeZ * bounds.SizeZ) / 2.0;
            double cameraDistance = radius * 1.5;

            Point3D camPos = new Point3D(centroid.X, centroid.Y - cameraDistance, centroid.Z + cameraDistance);
            oCam.Position = camPos;
            oCam.Width = cameraDistance;
            oCam.LookDirection = new Vector3D(0, 1, -1);
        }

        private void OnLmpEntrySelected(LmpEntryTreeViewModel lmpEntry)
        {
            var lmpFile = lmpEntry.LmpFileProperty;
            var entry = lmpFile.Directory[lmpEntry.Text];
            if (lmpEntry.Text.EndsWith(".tex")) {              
                SelectedNodeImage = TexDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);
            } else if (lmpEntry.Text.EndsWith(".vif")) {
                string texFilename = lmpEntry.Text.Replace(".vif", ".tex");
                var texEntry = lmpFile.Directory[texFilename];
                SelectedNodeImage = TexDecoder.Decode(lmpFile.FileData, texEntry.StartOffset, texEntry.Length);
                var animData = LoadFirstAnim(lmpFile);
                var log = new StringLogger();
                SelectedNodeModel = VifDecoder.Decode(log, lmpFile.FileData, entry.StartOffset, entry.Length, SelectedNodeImage, animData.Count == 0 ? null : animData.First(), 0);
                LogText += log.ToString();
                UpdateCamera(SelectedNodeModel, (OrthographicCamera)_selectedNodeCamera);
            } else if (lmpEntry.Text.EndsWith(".anm")) {
                var animData = AnmDecoder.Decode(lmpFile.FileData, entry.StartOffset, entry.Length);
                _skeletonViewModel.AnimData = animData;
                LogText = animData.ToString();
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

