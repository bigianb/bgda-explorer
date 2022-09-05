using JetBlackEngineLib.Data.World;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media.Media3D;

namespace WorldExplorer.WorldDefs;

public class ObjectManager
{
    private readonly ObjectDefinitions _defs;
    private readonly List<ObjectData> _objects = new();
    private readonly List<VisualObjectData> _visualObjects = new();

    public LevelViewModel LevelViewModel { get; }

    public ObjectManager(LevelViewModel levelViewModel)
    {
        LevelViewModel = levelViewModel;
        _defs = new ObjectDefinitions(this);
    }

    public void LoadScene(byte[] data, int offset, int length)
    {
        _visualObjects.Clear();
        _objects.Clear();

        _objects.AddRange(ObDecoder.Decode(data, offset, length));

        foreach (var obj in _objects)
        {
            ParseObject(obj);
        }
    }

    public void AddObjectsToScene(List<ModelVisual3D> scene)
    {
        foreach (var vod in _visualObjects)
        {
            vod.AddToScene(scene);
        }
    }

    public void RemoveObjectFromList(VisualObjectData vod)
    {
        if (_visualObjects.Contains(vod))
        {
            _visualObjects.Remove(vod);
        }
    }

    public VisualObjectData? ParseObject(ObjectData obj)
    {
        var vod = _defs.Parse(new()
        {
            ObjectData = obj,
            Offset = new Vector3D(obj.Floats[0] / 4, obj.Floats[1] / 4, obj.Floats[2] / 4),
            ZRotation = 22.5 * (obj.I6 >> 12)
        });
        if (vod != null)
        {
            _visualObjects.Add(vod);
        }
        return vod;
    }

    public VisualObjectData? HitTest(ModelVisual3D hitResult)
    {
        foreach (var vod in _visualObjects)
        {
            if (vod.Model != null && HitTestModel(vod.Model, hitResult))
            {
                return vod;
            }
        }

        return null;
    }

    private static bool HitTestModel(ModelVisual3D? obj, ModelVisual3D hitResult)
    {
        if (obj == null) return false;
        if (obj == hitResult)
        {
            return true;
        }

        foreach (var child in obj.Children)
        {
            if (child == hitResult)
            {
                return true;
            }

            if (child is ModelVisual3D visual3D)
            {
                if (HitTestModel(visual3D, hitResult))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public ObjectData? GetObjectByName(string name)
    {
        foreach (var obj in _objects)
        {
            if (obj.Name == name)
            {
                return obj;
            }
        }

        return null;
    }

    public bool TryGetObjectByName(string name, [NotNullWhen(true)] out ObjectData? result)
    {
        result = GetObjectByName(name);
        return result != null;
    }
}