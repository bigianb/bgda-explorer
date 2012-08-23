﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

using System.Windows.Media.Media3D;
using System.ComponentModel;
using WorldExplorer.DataLoaders;
using System.Windows.Media.Imaging;

namespace WorldExplorer
{
    public class ModelViewModel : INotifyPropertyChanged
    {
        private AnimData _animData;

        public AnimData AnimData
        {
            get { return _animData; }
            set
            {
                _animData = value;
                CurrentFrame = 0;
                UpdateModel();
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

        private List<List<VifDecoder.Chunk>> _vifData;

        public List<List<VifDecoder.Chunk>> VifData
        {
            get { return _vifData; }
            set
            {
                _vifData = value;
                UpdateModel();
                this.OnPropertyChanged("VifData");
            }
        }

        private void UpdateModel()
        {
            if (_vifData != null && _texture != null)
            {
                Model = VifDecoder.CreateModel3D(_vifData, _texture, _animData, CurrentFrame);
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
                UpdateModel();
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

        private Model3D _model;

        public Model3D Model
        {
            get { return _model; }
            set
            {
                _model = value;
                UpdateCamera(_model);
                InfoText = "Model Bounds: " + _model.Bounds.ToString();
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

        private void UpdateCamera(Model3D model)
        {
            OrthographicCamera oCam = (OrthographicCamera)_camera;

            var bounds = model.Bounds;
            Point3D centroid = new Point3D(0, 0, 0);
            double radius = Math.Sqrt(bounds.SizeX * bounds.SizeX + bounds.SizeY * bounds.SizeY + bounds.SizeZ * bounds.SizeZ) / 2.0;
            double cameraDistance = radius * 3.0;

            Point3D camPos = new Point3D(centroid.X, centroid.Y - cameraDistance, centroid.Z);
            oCam.Position = camPos;
            oCam.Width = cameraDistance;
            oCam.LookDirection = new Vector3D(0, 1, 0);
            oCam.UpDirection = new Vector3D(0, 0, 1);

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