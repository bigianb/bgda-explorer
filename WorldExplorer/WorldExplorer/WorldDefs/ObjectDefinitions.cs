using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using WorldExplorer.DataLoaders;
using WorldExplorer.Logging;
using WorldExplorer.Win3D;

namespace WorldExplorer.WorldDefs
{
    public class ObjectDefinitions
    {
        delegate VisualObjectData ParseObjectDelegate(VisualObjectData obj);

        readonly ObjectManager _manager;
        Dictionary<string, ParseObjectDelegate> _objectDefinitions = new Dictionary<string, ParseObjectDelegate>();
         
        public ObjectDefinitions(ObjectManager manager)
        {
            _manager = manager;

            SetupDefinitions();
        }

        private void SetupDefinitions()
        {
            // Lights
            AddDef("Ambient_Light", ParseLightObject);
            AddDef("Ambient_Light2", ParseLightObject);
            AddDef("charAmbient_Light", ParseLightObject);
            AddDef("charAmbient_Light2", ParseLightObject);
            AddDef("Directional_Light", ParseDirectionalObject);
            AddDef("Directional_Light2", ParseDirectionalObject);
            AddDef("Directional_LightD", ParseDirectionalObject);
            AddDef("Directional_LightD2", ParseDirectionalObject);
            AddDef("charDirectional_Light", ParseDirectionalObject);
            AddDef("charDirectional_Light2", ParseDirectionalObject);
            AddDef("charDirectional_LightD", ParseDirectionalObject);
            AddDef("charDirectional_LightD2", ParseDirectionalObject);
            AddDef("clearColor", ParseLightObject);

            AddDef("Trigger", ParseTriggerObject);
            AddDef("PushTrigger", ParsePushTriggerObject);

            AddDef("SaveCrystal", ParseSaveCrystalObject);

            // Characters
            AddDef("Bartender", ParseBartenderObject);
            AddDef("Garik", ParseGarikObject);
            AddDef("Drunk", ParseDrunkObject);
            AddDef("Hoochie", ParseHoochieObject);
            AddDef("CaravanGuard", ParseCaravanGuardObject);
            AddDef("Ethon", ParseEthonObject);
            AddDef("ShopKeep", ParseShopKeepObject);
            AddDef("Lecher", ParseLecherObject);
        }

        public VisualObjectData Parse(VisualObjectData obj)
        {
            ParseObjectDelegate del;
            if (_objectDefinitions.TryGetValue(obj.ObjectData.Name, out del))
            {
                return del(obj);
            }

            // Didn't match anything
            return ParseBaseObject(obj);
        }

        private VisualObjectData ParseBaseObject(VisualObjectData obj)
        {
            obj.Model = CreateBox(5, Color.FromArgb(255, 255, 247, 0));

            return obj;
        }

        #region Object Definitions

        // Ambient_Light
        private VisualObjectData ParseLightObject(VisualObjectData obj)
        {
            obj.Offset = new Vector3D(0, 0, 0);
            var sphere = new SphereVisual3D
            {
                Radius = 2.5
            };

            var color = ColorFromArray(obj.ObjectData.Floats);

            // Black light, don't show so that we can see actual colors
            if (color.R == 0 && color.G == 0 && color.B == 0)
                return null;

            sphere.Material = new DiffuseMaterial(new SolidColorBrush(color));

            obj.Model = sphere;
            return obj;
        }
        private VisualObjectData ParseDirectionalObject(VisualObjectData obj)
        {
            obj.Offset = new Vector3D(0, 0, 0);

            if (obj.ObjectData.Name.EndsWith("D") || obj.ObjectData.Name.EndsWith("D2"))
                return null;

            string dName = obj.ObjectData.Name;

            dName = dName.EndsWith("2") ? dName.Substring(0, dName.Length - 1) + "D2" : dName + "D";

            var directionObj = _manager.GetObjectByName(dName);

            if (directionObj == null)
            {
                Debug.Fail("Cannot find direction object");
                return null;
            }

            var color = ColorFromArray(obj.ObjectData.Floats);

            // Black light, don't show so that we can see actual colors
            if (color.R == 0 && color.G == 0 && color.B == 0)
                return null;

            var point1 = new Point3D(0, 0, 0);
            var point2 = new Point3D(directionObj.Floats[0] * 10, directionObj.Floats[1] * 10, directionObj.Floats[2] * 10);

            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(new SphereVisual3D
            {
                Center = point2,
                Radius = 1.0,
                Fill = new SolidColorBrush(color)
            });
            obj.Model.Children.Add(new ArrowVisual3D
            {
                Point1 = point2,
                Point2 = point1,
                Diameter = 0.5,
                Fill = new SolidColorBrush(color)
            });

            //obj.Model = sphere;
            return obj;
        }

        private VisualObjectData ParseTriggerObject(VisualObjectData obj)
        {
            var box = new BoxVisual3D();

            double tempValue = 0;

            if (!double.TryParse(obj.ObjectData.GetProperty("w"), out tempValue))
            {
                tempValue = 2.5 * 4;
            }
            box.Width = tempValue/4;
            box.Length = tempValue / 4;
            if (!double.TryParse(obj.ObjectData.GetProperty("h"), out tempValue))
            {
                tempValue = 2.5 * 4;
            }
            box.Height = tempValue/4;

            box.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 171, 0)));

            obj.Model = box;
            return obj;
        }

        private VisualObjectData ParsePushTriggerObject(VisualObjectData obj)
        {
            var sphere = new SphereVisual3D();

            double tempRadius = 0;

            if (!double.TryParse(obj.ObjectData.GetProperty("radius"), out tempRadius))
            {
                tempRadius = 2.5 * 4;
            }

            tempRadius =  tempRadius / 4;
            sphere.Radius = tempRadius;
            obj.Offset.Z += tempRadius / 2;

            sphere.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128,255,171,0)));

            obj.Model = sphere;
            return obj;
        }

        VisualObjectData ParseSaveCrystalObject(VisualObjectData obj)
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromOtherLmp("crystal.lmp", "book.vif", "book.tex"));

            return obj;
        }

        VisualObjectData ParseBartenderObject(VisualObjectData obj)
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromOtherLmp("bartend.lmp", "bartender.vif", "bartender.tex"));
            return obj;
        }

        VisualObjectData ParseGarikObject(VisualObjectData obj)
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromOtherLmp("garik.lmp", "garik.vif", "garik.tex"));
            return obj;
        }

        VisualObjectData ParseDrunkObject(VisualObjectData obj)
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromOtherLmp("drunk.lmp", "drunk.vif", "drunk.tex"));
            return obj;
        }

        VisualObjectData ParseHoochieObject(VisualObjectData obj)
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromOtherLmp("hoochie.lmp", "hoochie.vif", "hoochie.tex"));
            return obj;
        }

        VisualObjectData ParseCaravanGuardObject(VisualObjectData obj)
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromOtherLmp("cguard.lmp", "caravang.vif", "caravang.tex"));
            return obj;
        }

        VisualObjectData ParseEthonObject(VisualObjectData obj)
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromOtherLmp("ethon.lmp", "ethon.vif", "ethon.tex"));
            return obj;
        }

        VisualObjectData ParseShopKeepObject(VisualObjectData obj)
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromOtherLmp("bartley.lmp", "shopkeep1.vif", "shopkeep1.tex"));
            return obj;
        }

        VisualObjectData ParseLecherObject(VisualObjectData obj)
        {
            obj.Model = new ModelVisual3D();
            obj.Model.Children.Add(LoadModelFromOtherLmp("nebbish.lmp", "nebbish.vif", "nebbish.tex"));
            return obj;
        }

        #endregion

        // Utility Methods
        private void AddDef(string name, ParseObjectDelegate del)
        {
            _objectDefinitions.Add(name, del);
        }
        private Color ColorFromArray(float[] floats)
        {
            return Color.FromRgb((byte)floats[0], (byte)floats[1], (byte)floats[2]);
        }
        private Visual3D LoadModelFromOtherLmp(string lmpName, string file, string textureFile)
        {
            var par = _manager.LevelViewModel.WorldNode.Parent.Parent;

            if (par is GobTreeViewModel)
            {
                var gob = (GobTreeViewModel) par;
                foreach (LmpTreeViewModel child in gob.Children)
                {
                    if (string.Compare(child.Text, lmpName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        child.ForceLoadChildren();
                        var entry = child.LmpFileProperty.FindFile(file);
                        var texEntry = child.LmpFileProperty.FindFile(textureFile);

                        if (entry == null || texEntry == null)
                            return CreateBox(5, Color.FromRgb(255,0,0));

                        var tex = TexDecoder.Decode(child.LmpFileProperty.FileData, texEntry.StartOffset);

                        var logger = new StringLogger();
                        var vifModel = VifDecoder.Decode(
                            logger, 
                            child.LmpFileProperty.FileData, 
                            entry.StartOffset, 
                            entry.Length,
                            tex.PixelWidth, 
                            tex.PixelHeight);

                        var model = new ModelVisual3D
                        {
                            Content = Conversions.CreateModel3D(vifModel, tex, null, -1),
                            Transform = new ScaleTransform3D(1.0 / 4, 1.0 / 4, 1.0 / 4)
                        };
                        return model;
                    }
                }
            }

            return CreateBox(5, Color.FromRgb(255, 0, 0));
        }

        // Static Utility Methods
        private static BoxVisual3D CreateBox(double all, Color color)
        {
            return CreateBox(all, all, all, color);
        }
        private static BoxVisual3D CreateBox(double width, double height, double length, Color color)
        {
            var box = new BoxVisual3D
            {
                Width = width,
                Height = height,
                Length = length,
                Material = new DiffuseMaterial(new SolidColorBrush(color))
            };

            return box;
        }
    }
}
