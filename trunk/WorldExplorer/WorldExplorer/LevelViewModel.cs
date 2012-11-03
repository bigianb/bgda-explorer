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
using WorldExplorer.DataLoaders;
using System.Windows.Media.Imaging;
using WorldExplorer.DataModel;
using System.Windows.Media;
using WorldExplorer.WorldDefs;

namespace WorldExplorer
{
    public class LevelViewModel : BaseViewModel
    {
        private WorldData _worldData;
        private List<ModelVisual3D> _scene;
        private String _infoText;
        private ObjectManager _objectManager;

        public WorldFileTreeViewModel WorldNode { get; set; }
        public WorldData WorldData
        {
            get { return _worldData; }
            set
            {
                _worldData = value;
                RebuildScene();
            }
        }
        public String InfoText
        {
            get { return _infoText; }
            set
            {
                _infoText = value;
                this.OnPropertyChanged("InfoText");
            }
        }
        public List<ModelVisual3D> Scene
        {
            get { return _scene; }
            set
            {
                _scene = value;
                this.OnPropertyChanged("Scene");
            }
        }
        public ObjectManager ObjectManager
        {
            get { return _objectManager; }
        }

        public LevelViewModel(MainWindowViewModel mainViewWindow) : base(mainViewWindow)
        {
            _objectManager = new ObjectManager(this);
        }

        public void RebuildScene()
        {
            List<ModelVisual3D> scene = buildLights();

            foreach (var element in _worldData.worldElements)
            {
                ModelVisual3D mv3d = new ModelVisual3D();
                var model3D = VifDecoder.CreateModel3D(element.model.meshList, element.Texture, null, 0);
                mv3d.Content = model3D;

                var modelBounds = model3D.Bounds;

                Transform3DGroup transform3DGroup = new Transform3DGroup();

                transform3DGroup.Children.Add(new TranslateTransform3D(element.pos));
                Matrix3D mtx = Matrix3D.Identity;
                if (element.usesRotFlags) {
                    if ((element.xyzRotFlags & 4) == 4) {
                        // Flip x, y
                        mtx.M11 = 0;
                        mtx.M21 = 1;

                        mtx.M12 = 1;
                        mtx.M22 = 0;
                    }

                    if ((element.xyzRotFlags & 2) == 2) {
                        mtx.M11 = -mtx.M11;
                        mtx.M21 = -mtx.M21;
                    }

                    if ((element.xyzRotFlags & 1) == 1) {
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
                } else {
                    // Change handedness by reversing angle (sign on sin)
                    mtx.M11 = element.cosAlpha;
                    mtx.M21 = -element.sinAlpha;
                    mtx.M12 = element.sinAlpha;
                    mtx.M22 = element.cosAlpha;
                    if (element.negYaxis) {
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

            Scene = scene;

            LoadObjects();
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
            List<ModelVisual3D> scene = new List<ModelVisual3D>();
            ModelVisual3D ambientLight = new ModelVisual3D();
            ambientLight.Content = new AmbientLight(Color.FromRgb(0x80, 0x80, 0x80));
            scene.Add(ambientLight);
            ModelVisual3D directionalLight = new ModelVisual3D();
            directionalLight.Content = new DirectionalLight(Color.FromRgb(0x80, 0x80, 0x80), new Vector3D(0, -1, -1));
            scene.Add(directionalLight);

            return scene;
        }
    }
}
