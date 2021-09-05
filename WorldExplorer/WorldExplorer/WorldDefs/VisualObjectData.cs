using System.Collections.Generic;
using System.Windows.Media.Media3D;
using WorldExplorer.DataLoaders;

namespace WorldExplorer.WorldDefs
{
    public class VisualObjectData
    {
        public ModelVisual3D Model;
        public ObjectData ObjectData;
        public Vector3D Offset = new(0, 0, 0);
        public double zRotation;

        public void AddToScene(List<ModelVisual3D> scene)
        {
            Transform3DGroup transform3DGroup = new();

            if (zRotation != 0.0)
            {
                transform3DGroup.Children.Add(
                    new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), zRotation)));
            }

            transform3DGroup.Children.Add(new TranslateTransform3D(Offset));
            Model.Transform = transform3DGroup;

            scene.Add(Model);
        }
    }
}