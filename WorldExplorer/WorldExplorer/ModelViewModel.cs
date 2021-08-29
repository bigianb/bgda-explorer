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

using HelixToolkit.Wpf;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using WorldExplorer.DataExporters;
using WorldExplorer.DataModel;
using WorldExplorer.Win3D;

namespace WorldExplorer
{
    public class ModelViewModel : BaseViewModel
    {
        private AnimData _animData;
        private ModelView _modelView;

        public AnimData AnimData
        {
            get => _animData;
            set
            {
                _animData = value;
                CurrentFrame = 0;
                UpdateModel(false);
                OnPropertyChanged("AnimData");
                OnPropertyChanged("MaximumFrame");
            }
        }

        private WriteableBitmap _texture;

        public WriteableBitmap Texture
        {
            get => _texture;
            set => _texture = value;
        }

        private Model _vifModel;

        public Model VifModel
        {
            get => _vifModel;
            set
            {
                _vifModel = value;
                UpdateModel(true);
                OnPropertyChanged("VifModel");
            }
        }

        public ModelViewModel(MainWindowViewModel mainViewWindow) : base(mainViewWindow)
        {
            _modelView = MainViewModel.MainWindow.modelView;
        }

        private void UpdateModel(bool updateCamera)
        {
            if (_vifModel != null)
            {
                var newModel = (GeometryModel3D)Conversions.CreateModel3D(_vifModel.meshList, _texture, _animData, CurrentFrame);
                var container = new ModelVisual3D
                {
                    Content = newModel
                };

                if (_modelView.normalsBox.IsChecked.GetValueOrDefault())
                {
                    var normal = new MeshNormalsVisual3D
                    {
                        Mesh = (MeshGeometry3D)newModel.Geometry
                    };

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
            get => _animData == null ? 0 : _animData.NumFrames - 1;
            set
            {
            }
        }

        private int _currentFrame = 0;

        public int CurrentFrame
        {
            get => _currentFrame;
            set
            {
                _currentFrame = value;
                UpdateModel(false);
                OnPropertyChanged("CurrentFrame");
            }
        }

        private string _infoText;

        public string InfoText
        {
            get => _infoText;
            set
            {
                _infoText = value;
                OnPropertyChanged("InfoText");
            }

        }

        private ModelVisual3D _model;

        public ModelVisual3D Model
        {
            get => _model;
            set
            {
                _model = value;
                InfoText = "Model Bounds: " + _model.Content.Bounds.ToString();

                _modelView.viewport.Children.Remove(_modelView.modelObject);
                _modelView.modelObject = _model;
                _modelView.viewport.Children.Add(_modelView.modelObject);

                OnPropertyChanged("Model");
            }
        }

        private Transform3D _cameraTransform;

        public Transform3D CameraTransform
        {
            get => _cameraTransform;
            set
            {
                _cameraTransform = value;
                _camera.Transform = _cameraTransform;
                OnPropertyChanged("CameraTransform");
            }
        }

        private Camera _camera = new OrthographicCamera { Position = new Point3D(0, 10, -10), LookDirection = new Vector3D(0, -1, 1) };

        public Camera Camera
        {
            get => _camera;
            set
            {
                _camera = value;
                OnPropertyChanged("Camera");
            }
        }

        private void UpdateCamera(ModelVisual3D model)
        {
            var oCam = (OrthographicCamera)_camera;

            var bounds = model.Content.Bounds;
            var centroid = new Point3D(0, 0, 0);
            var radius = Math.Sqrt(bounds.SizeX * bounds.SizeX + bounds.SizeY * bounds.SizeY + bounds.SizeZ * bounds.SizeZ) / 2.0;
            var cameraDistance = radius * 3.0;

            var camPos = new Point3D(centroid.X, centroid.Y - cameraDistance, centroid.Z);
            oCam.Position = camPos;
            oCam.Width = cameraDistance;
            oCam.LookDirection = new Vector3D(0, 1, 0);
            oCam.UpDirection = new Vector3D(0, 0, 1);
        }

        public void ShowExportForPosedModel()
        {
            if (Model == null)
            {
                System.Windows.MessageBox.Show("No model currently loaded.", "Error", MessageBoxButton.OK);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "OBJ File|*.obj|GLTF File|*.gltf"
            };
            // Select gltf by default
            dialog.FilterIndex = 2;
            if (dialog.ShowDialog() != DialogResult.OK) return;

            var ext = Path.GetExtension(dialog.FileName).ToUpperInvariant();
            switch (ext)
            {
                case ".OBJ":
                    VifObjExporter.WritePosedObj(dialog.FileName,
                                   VifModel,
                                   Texture,
                                   AnimData,
                                   CurrentFrame,
                                   1.0);
                    break;
                case ".GLTF":
                    VifGltfExporter.WritePosedGltf(dialog.FileName,
                                   VifModel,
                                   Texture,
                                   AnimData,
                                   CurrentFrame,
                                   1.0);
                    break;
            }
        }
    }
}
