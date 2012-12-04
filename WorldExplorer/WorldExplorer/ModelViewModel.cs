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
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.ComponentModel;
using HelixToolkit;
using WorldExplorer.DataLoaders;
using System.Windows.Media.Imaging;
using WorldExplorer.DataModel;

namespace WorldExplorer
{
    public class ModelViewModel : BaseViewModel
    {
        private AnimData _animData;
        private ModelView _modelView;

        public AnimData AnimData
        {
            get { return _animData; }
            set
            {
                _animData = value;
                CurrentFrame = 0;
                UpdateModel(false);
                this.OnPropertyChanged("AnimData");
                this.OnPropertyChanged("MaximumFrame");
            }
        }

        private WriteableBitmap _texture;

        public WriteableBitmap Texture
        {
            get { return _texture; }
            set
            {
                _texture = value;
            }
        }

        private Model _vifModel;

        public Model VifModel
        {
            get { return _vifModel; }
            set
            {
                _vifModel = value;
                UpdateModel(true);
                this.OnPropertyChanged("VifModel");
            }
        }

        public ModelViewModel(MainWindowViewModel mainViewWindow) : base(mainViewWindow)
        {
            _modelView = MainViewModel.MainWindow.modelView;
        }

        private void UpdateModel(Boolean updateCamera)
        {
            if (_vifModel != null)
            {
                var newModel = (GeometryModel3D)VifDecoder.CreateModel3D(_vifModel.meshList, _texture, _animData, CurrentFrame);
                var container = new ModelVisual3D();
                container.Content = newModel;

                if (_modelView.normalsBox.IsChecked.GetValueOrDefault())
                {
                    var normal = new MeshNormals3D();
                    normal.Mesh = (MeshGeometry3D)newModel.Geometry;

                    container.Children.Add(normal);
                }

                Model = container;

                if (updateCamera)
                {
                    UpdateCamera(_model);
                }
            }
        }

        public int MaximumFrame
        {
            get { return _animData == null ? 0 : _animData.NumFrames-1; }
            set { }
        }

        private int _currentFrame = 0;

        public int CurrentFrame
        {
            get { return _currentFrame; }
            set {
                _currentFrame = value;
                UpdateModel(false);
                this.OnPropertyChanged("CurrentFrame");
            }
        }

        private String _infoText;

        public String InfoText
        {
            get { return _infoText; }
            set
            {
                _infoText = value;
                this.OnPropertyChanged("InfoText");
            }

        }

        private ModelVisual3D _model;

        public ModelVisual3D Model
        {
            get { return _model; }
            set
            {
                _model = value;
                InfoText = "Model Bounds: " + _model.Content.Bounds.ToString();

                _modelView.viewport.Children.Remove(_modelView.modelObject);
                _modelView.modelObject = _model;
                _modelView.viewport.Children.Add(_modelView.modelObject);

                this.OnPropertyChanged("Model");
            }
        }

        private Transform3D _cameraTransform;

        public Transform3D CameraTransform
        {
            get { return _cameraTransform; }
            set
            {
                _cameraTransform = value;
                _camera.Transform = _cameraTransform;
                this.OnPropertyChanged("CameraTransform");
            }
        }

        private Camera _camera = new OrthographicCamera { Position = new Point3D(0, 10, -10), LookDirection = new Vector3D(0, -1, 1) };

        public Camera Camera
        {
            get { return _camera; }
            set
            {
                _camera = value;
                this.OnPropertyChanged("Camera");
            }
        }

        private void UpdateCamera(ModelVisual3D model)
        {
            OrthographicCamera oCam = (OrthographicCamera)_camera;

            var bounds = model.Content.Bounds;
            Point3D centroid = new Point3D(0, 0, 0);
            double radius = Math.Sqrt(bounds.SizeX * bounds.SizeX + bounds.SizeY * bounds.SizeY + bounds.SizeZ * bounds.SizeZ) / 2.0;
            double cameraDistance = radius * 3.0;

            Point3D camPos = new Point3D(centroid.X, centroid.Y - cameraDistance, centroid.Z);
            oCam.Position = camPos;
            oCam.Width = cameraDistance;
            oCam.LookDirection = new Vector3D(0, 1, 0);
            oCam.UpDirection = new Vector3D(0, 0, 1);
        }
    }
}
