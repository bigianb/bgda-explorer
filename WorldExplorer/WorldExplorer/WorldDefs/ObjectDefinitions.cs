using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
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

            // == RTA Objects
            // Characters
            AddDef("NPC", ParseObjectWithLumpProp("name"));
            AddDef("bflya", ParseToMappedLmp("BFLYA.LMP", "BFLYA.vif", "BFLYA.tex"));
            AddDef("bflyb", ParseToMappedLmp("BFLYB.LMP", "BFLYB.vif", "BFLYB.tex"));
            AddDef("Kobold", ParseToMappedLmp("KOBOLD.LMP", "kobold.vif", "kobold.tex"));
            AddDef("Bullfrog", ParseToMappedLmp("BULLFROG.LMP", "bullfrog.vif", "bullfrog.tex"));
            AddDef("Rat", ParseToMappedLmp("RAT.LMP", "rat.vif", "rat.tex"));
            AddDef("Seagull", ParseToMappedLmp("SEAGULL.LMP", "seagull.vif", "seagull.tex"));
            AddDef("fgoblin", ParseToMappedLmp("FGOBLIN.LMP", "firegoblin.vif", "firegoblin.tex"));
            AddDef("Samurai", ParseToMappedLmp("SAMURAI.LMP", "Samurai.vif", "Samurai.tex"));
            AddDef("Player1", ParseToMappedLmp("SELECT.LMP", "im_bodyv1.vif", "im_bodyv1.tex"));

            // Objects
            AddDef("Savepoint", ParseToMappedLmp("SAVEPNT.LMP", "savepoint.vif", "savepoint.tex"));
            AddDef("Portal", ParseObjectWithLumpProp("lump"));
            AddDef("orkbox", ParseObjectWithLumpProp("lumpname"));
            AddDef("lavakeg", ParseObjectWithLumpProp("lumpname"));
            AddDef("gearbox", ParseObjectWithLumpProp("lumpname"));
            AddDef("PowderKeg", ParseToMappedLmp("POWDERK.LMP", "powderkeg.vif", "powderkeg.tex"));
            AddDef("Barrel", ParseToMappedLmp("BARREL.LMP", "barrel.vif", "barrel.tex"));
            AddDef("Wingedv", ParseToMappedLmp("BARREL.LMP", "barrel.vif", "barrel.tex"));
        }

        public VisualObjectData Parse(VisualObjectData obj)
        {
            if (_objectDefinitions.TryGetValue(obj.ObjectData.Name, out var del))
            {
                try
                {
                    return del(obj);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Error parsing object definition\r\n" + ex);
                    obj.Model = new ModelVisual3D();
                    obj.Model.Children.Add(CreateBox(5, Color.FromRgb(255, 0, 0)));
                    return obj;
                }
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
            {
                return null;
            }

            sphere.Material = new DiffuseMaterial(new SolidColorBrush(color));

            obj.Model = sphere;
            return obj;
        }
        private VisualObjectData ParseDirectionalObject(VisualObjectData obj)
        {
            obj.Offset = new Vector3D(0, 0, 0);

            if (obj.ObjectData.Name.EndsWith("D") || obj.ObjectData.Name.EndsWith("D2"))
            {
                return null;
            }

            var dName = obj.ObjectData.Name;

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
            {
                return null;
            }

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


            if (!double.TryParse(obj.ObjectData.GetProperty("w"), out var tempValue))
            {
                tempValue = 2.5 * 4;
            }
            box.Width = tempValue / 4;
            box.Length = tempValue / 4;
            if (!double.TryParse(obj.ObjectData.GetProperty("h"), out tempValue))
            {
                tempValue = 2.5 * 4;
            }
            box.Height = tempValue / 4;

            box.BackMaterial = box.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 171, 0)));
            

            obj.Model = box;
            return obj;
        }

        private VisualObjectData ParsePushTriggerObject(VisualObjectData obj)
        {
            var sphere = new SphereVisual3D();


            if (!double.TryParse(obj.ObjectData.GetProperty("radius"), out var tempRadius))
            {
                tempRadius = 2.5 * 4;
            }

            tempRadius = tempRadius / 4;
            sphere.Radius = tempRadius;
            obj.Offset.Z += tempRadius / 2;

            sphere.BackMaterial = sphere.Material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 171, 0)));

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

        ParseObjectDelegate ParseObjectWithLumpProp(string propName)
        {
            return new ParseObjectDelegate((obj) =>
            {
                var props = obj.ObjectData.Properties.Select(e => e.Split('=')).ToDictionary(e => e[0], e => e.Length > 1 ? e[1] : e[0]);
                if (props.TryGetValue(propName, out var lump) && !string.IsNullOrEmpty(lump))
                {
                    lump = lump.Trim().TrimQuotes();
                    obj.Model = new ModelVisual3D();
                    obj.Model.Children.Add(LoadModelFromExternalLmp(lump + ".lmp", lump + ".vif", lump + ".tex"));
                }
                else
                    obj.Model.Children.Add(CreateBox(5, Color.FromRgb(255, 0, 0)));
                return obj;
            });
        }

        ParseObjectDelegate ParseToMappedLmp(string lmpFile, string vifFile, string texFile)
        {
            return new ParseObjectDelegate((obj) =>
            {
                obj.Model = new ModelVisual3D();
                obj.Model.Children.Add(LoadModelFromExternalLmp(lmpFile, vifFile, texFile));
                return obj;
            });
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

        private Visual3D LoadModelFromExternalLmp(string lmpName, string file, string textureFile)
        {
            var lmpPath = Path.Combine(_manager.LevelViewModel.MainViewModel.World.DataPath, lmpName.ToUpperInvariant());

            if (File.Exists(lmpPath)) {
                // TODO: We can cache this to reduce memory usage
                var data = File.ReadAllBytes(lmpPath);
                var externalLmp = new LmpFile(_manager.LevelViewModel.MainViewModel.World.EngineVersion, lmpName, data, 0, data.Length);
                externalLmp.ReadDirectory();

                var entry = externalLmp.FindFile(file);
                var texEntry = externalLmp.FindFile(textureFile);

                if (entry == null || texEntry == null)
                {
                    return CreateBox(5, Color.FromRgb(255, 0, 0));
                }

                var tex = TexDecoder.Decode(externalLmp.FileData, texEntry.StartOffset);

                var logger = new StringLogger();
                var vifModel = VifDecoder.Decode(
                    logger,
                    externalLmp.FileData,
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

            

            return CreateBox(5, Color.FromRgb(255, 0, 0));
        }

        private Visual3D LoadModelFromOtherLmp(string lmpName, string file, string textureFile)
        {
            var par = _manager.LevelViewModel.WorldNode.Parent.Parent;

            if (par is GobTreeViewModel)
            {
                var gob = (GobTreeViewModel)par;
                foreach (LmpTreeViewModel child in gob.Children)
                {
                    if (string.Compare(child.Text, lmpName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        child.ForceLoadChildren();
                        var entry = child.LmpFileProperty.FindFile(file);
                        var texEntry = child.LmpFileProperty.FindFile(textureFile);

                        if (entry == null || texEntry == null)
                        {
                            return CreateBox(5, Color.FromRgb(255, 0, 0));
                        }

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
