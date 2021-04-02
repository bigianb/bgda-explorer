using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using WorldExplorer.DataLoaders;

namespace WorldExplorer.WorldDefs
{
    public class VisualObjectData
    {
        public ObjectData ObjectData;
        public Vector3D Offset = new Vector3D(0, 0, 0);
        public double zRotation;
        public ModelVisual3D Model;

        public void AddToScene(List<ModelVisual3D> scene)
        {
            Transform3DGroup transform3DGroup = new Transform3DGroup();
           
            if (zRotation != 0.0)
            {
                transform3DGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), zRotation)));
            }
            transform3DGroup.Children.Add(new TranslateTransform3D(Offset));
            Model.Transform = transform3DGroup; 
            
            scene.Add(Model);
        }
    }
}
