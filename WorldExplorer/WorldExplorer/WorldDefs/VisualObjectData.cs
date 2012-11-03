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

namespace WorldExplorer.WorldDefs
{
    public class VisualObjectData
    {
        public ObjectData ObjectData;
        public Vector3D Offset = new Vector3D(0, 0, 0);
        public ModelVisual3D Model;
        
        public void AddToScene(LevelViewModel levelView)
        {
            Model.Transform = new TranslateTransform3D(Offset);
            levelView.Scene.Add(Model);
        }
    }
}
