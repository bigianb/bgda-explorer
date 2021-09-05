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
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WorldExplorer.DataLoaders;
using WorldExplorer.DataModel;
using WorldExplorer.Logging;
using WorldExplorer.Win3D;
using WorldExplorer.WorldDefs;

namespace WorldExplorer
{
    public class LevelViewModel : BaseViewModel
    {
        private LmpFile? _domeLmp;
        private bool _enableLights = true;
        private string? _infoText;
        private List<ModelVisual3D>? _scene;
        private WorldElementTreeViewModel? _selectedElement;
        private VisualObjectData? _selectedObject;
        private Rect3D _worldBounds = Rect3D.Empty;
        private WorldData? _worldData;
        private WorldFileTreeViewModel? _worldNode;

        public Rect3D WorldBounds
        {
            get => _worldBounds;
            private set
            {
                _worldBounds = value;
                OnPropertyChanged(nameof(WorldBounds));
            }
        }

        public WorldFileTreeViewModel? WorldNode
        {
            get => _worldNode;
            set
            {
                _worldNode = value;
                OnPropertyChanged(nameof(WorldNode));
            }
        }

        public WorldData? WorldData
        {
            get => _worldData;
            set
            {
                _worldData = value;
                NewWorldLoaded();
                OnPropertyChanged(nameof(WorldData));
            }
        }

        public string? InfoText
        {
            get => _infoText;
            set
            {
                _infoText = value;
                OnPropertyChanged(nameof(InfoText));
            }
        }

        public List<ModelVisual3D>? Scene
        {
            get => _scene;
            set
            {
                _scene = value;
                OnPropertyChanged(nameof(Scene));
            }
        }

        public bool EnableLevelSpecifiedLights
        {
            get => _enableLights;
            set
            {
                _enableLights = value;
                RebuildScene();
                OnPropertyChanged(nameof(EnableLevelSpecifiedLights));
            }
        }

        public VisualObjectData? SelectedObject
        {
            get => _selectedObject;
            set
            {
                _selectedObject = value;
                OnPropertyChanged(nameof(SelectedObject));
            }
        }

        public WorldElementTreeViewModel? SelectedElement
        {
            get => _selectedElement;
            set
            {
                _selectedElement = value;
                OnPropertyChanged("SelectedElement");
            }
        }

        public ObjectManager ObjectManager { get; }

        public LevelViewModel(MainWindowViewModel mainViewWindow) : base(mainViewWindow)
        {
            ObjectManager = new ObjectManager(this);
        }

        public event EventHandler? SceneUpdated;

        public void RebuildScene()
        {
            List<ModelVisual3D> scene = new();
            AddLights(EnableLevelSpecifiedLights, scene);

            var worldBounds = Rect3D.Empty;

            if (_worldData != null)
            {
                foreach (var element in _worldData.worldElements)
                {
                    ModelVisual3D mv3d = new();
                    var model3D = Conversions.CreateModel3D(element.model.MeshList, element.Texture, null, 0);
                    mv3d.Content = model3D;

                    var modelBounds = model3D.Bounds;

                    worldBounds.Union(modelBounds);

                    Transform3DGroup transform3DGroup = new();

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
            }

            ObjectManager.AddObjectsToScene(scene);

            AddSkyDomeModels(scene);

            WorldBounds = worldBounds;
            Scene = scene;
        }

        private void ResetState()
        {
            _domeLmp = null;
        }

        private void NewWorldLoaded()
        {
            ResetState();

            LoadObjects();
            LoadSkyDome();
            RebuildScene();

            SceneUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void LoadObjects()
        {
            if (WorldNode == null)
                return;
            
            if (WorldNode.LmpFileProperty.Directory.ContainsKey("objects.ob"))
            {
                var obNode = WorldNode.LmpFileProperty.Directory["objects.ob"];

                ObjectManager.LoadScene(WorldNode.LmpFileProperty.FileData, obNode.StartOffset, obNode.Length);
            }
        }

        private void LoadSkyDome()
        {
            var gobFile = WorldNode?.Parent?.Parent as GobTreeViewModel;
            var domeFile = gobFile?.Children.OfType<LmpTreeViewModel>()
                .FirstOrDefault(e => e.Text.Equals("dome.lmp", StringComparison.OrdinalIgnoreCase));
            if (domeFile == null)
            {
                _domeLmp = null;
                return;
            }

            if (domeFile.HasDummyChild)
            {
                domeFile.ForceLoadChildren();
            }

            _domeLmp = domeFile.LmpFileProperty;
        }

        private void AddSkyDomeModels(List<ModelVisual3D> scene)
        {
            if (_domeLmp == null)
            {
                return;
            }

            var vifChildren = _domeLmp.Directory.Keys
                .Where(e => e.EndsWith(".vif", StringComparison.OrdinalIgnoreCase));

            foreach (var vifFileName in vifChildren)
            {
                if (!_domeLmp.Directory.TryGetValue(vifFileName, out var vifEntry))
                    continue;
                var texFilename = Path.GetFileNameWithoutExtension(vifFileName) + ".tex";
                
                if (!_domeLmp.Directory.TryGetValue(texFilename.ToLowerInvariant(), out var texEntry))
                    // Couldn't find the tex file, ignore this vif entry
                    continue;

                var selectedNodeImage =
                    TexDecoder.Decode(_domeLmp.FileData.AsSpan().Slice(texEntry.StartOffset, texEntry.Length));
                StringLogger log = new();

                Model model = new(VifDecoder.Decode(
                    log,
                    _domeLmp.FileData.AsSpan().Slice(vifEntry.StartOffset, vifEntry.Length),
                    selectedNodeImage?.PixelWidth ?? 0,
                    selectedNodeImage?.PixelHeight ?? 0));

                var newModel =
                    (GeometryModel3D)Conversions.CreateModel3D(model.MeshList, selectedNodeImage);
                scene.Add(new ModelVisual3D {Content = newModel});
            }
        }

        private void AddLights(bool enableLevelSpecifiedLights, List<ModelVisual3D> scene)
        {
            var ambientColor = Color.FromRgb(0x80, 0x80, 0x80);
            var directionalColor = Color.FromRgb(0x80, 0x80, 0x80);
            Vector3D directionalAngle = new(0, -1, -1);

            // Attempt to find the correct values from objects
            if (enableLevelSpecifiedLights)
            {
                if (ObjectManager.TryGetObjectByName("Ambient_Light", out var ambientLightObj))
                {
                    ambientColor = Color.FromRgb((byte)ambientLightObj.Floats[0], (byte)ambientLightObj.Floats[1],
                        (byte)ambientLightObj.Floats[2]);
                }

                if (ObjectManager.TryGetObjectByName("Directional_Light", out var dirLightColorObj))
                {
                    directionalColor = Color.FromRgb((byte)dirLightColorObj.Floats[0],
                        (byte)dirLightColorObj.Floats[1], (byte)dirLightColorObj.Floats[2]);
                }

                if (ObjectManager.TryGetObjectByName("Directional_LightD", out var dirLightAngleObj))
                {
                    directionalAngle = new Vector3D(-dirLightAngleObj.Floats[0], -dirLightAngleObj.Floats[1],
                        -dirLightAngleObj.Floats[2]);
                }
            }

            ModelVisual3D ambientLight = new() {Content = new AmbientLight(ambientColor)};
            ModelVisual3D directionalLight = new() {Content = new DirectionalLight(directionalColor, directionalAngle)};
            scene.Add(ambientLight);
            scene.Add(directionalLight);
        }
    }
}