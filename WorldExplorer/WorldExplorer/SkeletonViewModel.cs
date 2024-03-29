﻿/*  Copyright (C) 2012 Ian Brown

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

using JetBlackEngineLib.Data.Animation;
using System;
using System.Windows.Media.Media3D;

namespace WorldExplorer;

public class SkeletonViewModel : BaseViewModel
{
    private AnimData? _animData;

    private Camera _camera = new OrthographicCamera
    {
        Position = new Point3D(0, 10, -10), LookDirection = new Vector3D(0, -1, 1)
    };

    private Transform3D _cameraTransform = Transform3D.Identity;

    private int _currentFrame;

    private Model3D? _model;

    public AnimData? AnimData
    {
        get => _animData;
        set
        {
            _animData = value;
            CurrentFrame = 0;
            UpdateModel();
            OnPropertyChanged("AnimData");
            OnPropertyChanged("MaximumFrame");
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
            UpdateModel();
            OnPropertyChanged("CurrentFrame");
        }
    }

    public Model3D? Model
    {
        get => _model;
        set
        {
            _model = value;
            UpdateCamera(_model);
            OnPropertyChanged("Model");
        }
    }

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

    public Camera Camera
    {
        get => _camera;
        set
        {
            _camera = value;
            OnPropertyChanged("Camera");
        }
    }

    public SkeletonViewModel(MainWindowViewModel mainViewWindow) : base(mainViewWindow)
    {
    }

    private void UpdateModel()
    {
        Model = SkeletonProcessor.GetSkeletonModel(_animData, CurrentFrame);
    }

    private void UpdateCamera(Model3D? model)
    {
        if (model == null)
        {
            return;
        }

        var oCam = (OrthographicCamera)_camera;

        var bounds = model.Bounds;
        //Point3D centroid = new Point3D(bounds.X + bounds.SizeX / 2.0, bounds.Y + bounds.SizeY / 2.0, bounds.Z + bounds.SizeZ / 2.0);
        Point3D centroid = new(0, 0, 0);
        var radius =
            Math.Sqrt((bounds.SizeX * bounds.SizeX) + (bounds.SizeY * bounds.SizeY) +
                      (bounds.SizeZ * bounds.SizeZ)) /
            2.0;
        var cameraDistance = radius * 2.0;

        Point3D camPos = new(centroid.X, centroid.Y - cameraDistance, centroid.Z + cameraDistance);
        oCam.Position = camPos;
        oCam.Width = cameraDistance;
        oCam.LookDirection = new Vector3D(0, 1, -1);
    }
}