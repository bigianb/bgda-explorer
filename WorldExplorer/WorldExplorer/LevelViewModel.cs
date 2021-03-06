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

using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;
using WorldExplorer.Win3D;
using WorldExplorer.WorldDefs;

namespace WorldExplorer
{
    public class LevelViewModel : BaseViewModel
    {
        private WorldData _worldData;
        private List<ModelVisual3D> _scene;
        private string _infoText;
        private ObjectManager _objectManager;

        public WorldFileTreeViewModel WorldNode
        {
            get; set;
        }
        public WorldData WorldData
        {
            get => _worldData;
            set
            {
                _worldData = value;
                NewWorldLoaded();
            }
        }
        public string InfoText
        {
            get => _infoText;
            set
            {
                _infoText = value;
                OnPropertyChanged("InfoText");
            }
        }
        public List<ModelVisual3D> Scene
        {
            get => _scene;
            set
            {
                _scene = value;
                OnPropertyChanged("Scene");
            }
        }
        public ObjectManager ObjectManager => _objectManager;

        public LevelViewModel(MainWindowViewModel mainViewWindow) : base(mainViewWindow)
        {
            _objectManager = new ObjectManager(this);
        }

        public void RebuildScene()
        {
            var scene = buildLights();

            foreach (var element in _worldData.worldElements)
            {
                var mv3d = new ModelVisual3D();
                var model3D = Conversions.CreateModel3D(element.model.meshList, element.Texture, null, 0);
                mv3d.Content = model3D;

                var modelBounds = model3D.Bounds;

                var transform3DGroup = new Transform3DGroup();

                transform3DGroup.Children.Add(new TranslateTransform3D(element.pos));
                var mtx = Matrix3D.Identity;
                if (element.usesRotFlags)
                {
                    if ((element.xyzRotFlags & 4) == 4)
                    {
                        // Flip x, y
                        mtx.M11 = 0;
                        mtx.M21 = 1;

                        mtx.M12 = 1;
                        mtx.M22 = 0;
                    }

                    if ((element.xyzRotFlags & 2) == 2)
                    {
                        mtx.M11 = -mtx.M11;
                        mtx.M21 = -mtx.M21;
                    }

                    if ((element.xyzRotFlags & 1) == 1)
                    {
                        mtx.M12 = -mtx.M12;
                        mtx.M22 = -mtx.M22;
                    }

                    if (element.xyzRotFlags == 2)
                    {
                        mtx.M11 = -mtx.M11;
                        mtx.M12 = -mtx.M12;
                        mtx.M21 = -mtx.M21;
                        mtx.M22 = -mtx.M22;
                    }

                    if (element.xyzRotFlags == 1)
                    {
                        mtx.M12 = -mtx.M12;
                        mtx.M22 = -mtx.M22;
                        mtx.M11 = -mtx.M11;
                        mtx.M21 = -mtx.M21;
                    }
                }
                else
                {
                    // Change handedness by reversing angle (sign on sin)
                    mtx.M11 = element.cosAlpha;
                    mtx.M21 = -element.sinAlpha;
                    mtx.M12 = element.sinAlpha;
                    mtx.M22 = element.cosAlpha;
                    if (element.negYaxis)
                    {
                        // Should this be col1 due to handed change?
                        mtx.M12 = -mtx.M12;
                        mtx.M22 = -mtx.M22;
                    }
                }
                if (!mtx.IsIdentity)
                {
                    transform3DGroup.Children.Add(new MatrixTransform3D(mtx));
                }
                mv3d.Transform = transform3DGroup;

                scene.Add(mv3d);
            }

            ObjectManager.AddObjectsToScene(scene);

            Scene = scene;
        }

        private void NewWorldLoaded()
        {
            LoadObjects();
            RebuildScene();
        }
        private void LoadObjects()
        {
            if (WorldNode.LmpFileProperty.Directory.ContainsKey("objects.ob"))
            {
                var obNode = WorldNode.LmpFileProperty.Directory["objects.ob"];

                _objectManager.LoadScene(WorldNode.LmpFileProperty.FileData, obNode.StartOffset, obNode.Length);
            }
        }

        private List<ModelVisual3D> buildLights()
        {
            var scene = new List<ModelVisual3D>();
            var ambientLight = new ModelVisual3D
            {
                Content = new AmbientLight(Color.FromRgb(0x80, 0x80, 0x80))
            };
            scene.Add(ambientLight);
            var directionalLight = new ModelVisual3D
            {
                Content = new DirectionalLight(Color.FromRgb(0x80, 0x80, 0x80), new Vector3D(0, -1, -1))
            };
            scene.Add(directionalLight);

            return scene;
        }
    }
}
