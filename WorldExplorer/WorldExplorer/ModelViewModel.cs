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
using JetBlackEngineLib.Data.Animation;
using JetBlackEngineLib.Data.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using WorldExplorer.DataExporters;
using WorldExplorer.Win3D;
using MessageBox = System.Windows.MessageBox;

namespace WorldExplorer;

public class ModelViewModel : BaseViewModel
{
    private readonly ModelView _modelView;
    private AnimData? _animData;

    private Camera _camera = new OrthographicCamera
    {
        Position = new Point3D(0, 10, -10), LookDirection = new Vector3D(0, -1, 1)
    };

    private Transform3D _cameraTransform = Transform3D.Identity;

    private int _currentFrame;

    private string? _infoText;

    private ModelVisual3D? _model;

    private Model? _vifModel;

    public AnimData? AnimData
    {
        get => _animData;
        set
        {
            _animData = value;
            CurrentFrame = 0;
            UpdateModel(false);
            OnPropertyChanged(nameof(AnimData));
            OnPropertyChanged(nameof(MaximumFrame));
        }
    }

    public WriteableBitmap? Texture { get; set; }

    public Model? VifModel
    {
        get => _vifModel;
        set
        {
            _vifModel = value;
            UpdateModel(true);
            OnPropertyChanged(nameof(VifModel));
        }
    }

    public int MaximumFrame
    {
        get => _animData == null ? 0 : _animData.NumFrames - 1;
        // set { }
    }

    public int CurrentFrame
    {
        get => _currentFrame;
        set
        {
            _currentFrame = value;
            UpdateModel(false);
            OnPropertyChanged(nameof(CurrentFrame));
        }
    }

    public string?InfoText
    {
        get => _infoText;
        set
        {
            _infoText = value;
            OnPropertyChanged(nameof(InfoText));
        }
    }

    public ModelVisual3D? Model
    {
        get => _model;
        set
        {
            _model = value;

            if (_model != null)
            {
                InfoText = $"Model Bounds: {_model.Content.Bounds}";
                _modelView.viewport.Children.Remove(_modelView.modelObject);
                _modelView.modelObject = _model;
                _modelView.viewport.Children.Add(_modelView.modelObject);
            }
            else
            {
                InfoText = null;
            }

            OnPropertyChanged(nameof(Model));
        }
    }

    public Transform3D CameraTransform
    {
        get => _cameraTransform;
        set
        {
            _cameraTransform = value;
            _camera.Transform = _cameraTransform;
            OnPropertyChanged(nameof(CameraTransform));
        }
    }

    public Camera Camera
    {
        get => _camera;
        set
        {
            _camera = value;
            OnPropertyChanged(nameof(Camera));
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
            var newModel =
                (GeometryModel3D)Conversions.CreateModel3D(_vifModel.MeshList, Texture, _animData, CurrentFrame);
            ModelVisual3D container = new() {Content = newModel};

            if (_modelView.normalsBox.IsChecked.GetValueOrDefault())
            {
                MeshNormalsVisual3D normal = new() {Mesh = (MeshGeometry3D)newModel.Geometry};

                container.Children.Add(normal);
            }

            Model = container;

            if (updateCamera && _model != null)
            {
                UpdateCamera(_model);
            }
        }
    }

    private void UpdateCamera(ModelVisual3D model)
    {
        var oCam = (OrthographicCamera)_camera;

        var bounds = model.Content.Bounds;
        Point3D centroid = new(0, 0, 0);
        var radius =
            Math.Sqrt((bounds.SizeX * bounds.SizeX) + (bounds.SizeY * bounds.SizeY) +
                      (bounds.SizeZ * bounds.SizeZ)) /
            2.0;
        var cameraDistance = radius * 3.0;

        Point3D camPos = new(centroid.X, centroid.Y - cameraDistance, centroid.Z);
        oCam.Position = camPos;
        oCam.Width = cameraDistance;
        oCam.LookDirection = new Vector3D(0, 1, 0);
        oCam.UpDirection = new Vector3D(0, 0, 1);
    }

    public void ShowExportForPosedModel()
    {
        if (Model == null || VifModel == null)
        {
            MessageBox.Show("No model currently loaded.", "Error", MessageBoxButton.OK);
            return;
        }

        SaveFileDialog dialog = new()
        {
            Filter = "GLTF File|*.gltf|OBJ File|*.obj",
            // Select gltf by default
            FilterIndex = 1,
            FileName = "some-model.gltf"
        };
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var ext = Path.GetExtension(dialog.FileName).ToUpperInvariant();
        IVifExporter? exporter = ext switch
        {
            ".OBJ" => new VifObjExporter(),
            ".GLTF" => new VifGltfExporter(),
            _ => null
        };
        if (exporter == null)
        {
            MessageBox.Show("Unknown file format.", "Error", MessageBoxButton.OK);
            return;
        }

        exporter.SaveToFile(dialog.FileName, VifModel, Texture, AnimData, CurrentFrame);
    }
}