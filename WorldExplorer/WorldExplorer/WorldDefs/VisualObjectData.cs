using JetBlackEngineLib.Data.World;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace WorldExplorer.WorldDefs;

public class VisualObjectData
{
    public ModelVisual3D? Model;
    public ObjectData? ObjectData;
    public Vector3D Offset = new(0, 0, 0);
    public double ZRotation;

    public void AddToScene(List<ModelVisual3D> scene)
    {
        if (Model == null)
        {
            return;
        }
            
        Transform3DGroup transform3DGroup = new();

        if (ZRotation != 0.0)
        {
            transform3DGroup.Children.Add(
                new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), ZRotation)));
        }

        transform3DGroup.Children.Add(new TranslateTransform3D(Offset));

        Model.Transform = transform3DGroup;
        scene.Add(Model);
    }
}