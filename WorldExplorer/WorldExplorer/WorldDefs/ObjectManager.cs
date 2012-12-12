using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using WorldExplorer.DataLoaders;

namespace WorldExplorer.WorldDefs
{
    public class ObjectManager
    {
        readonly LevelViewModel _levelViewModel;
        readonly List<VisualObjectData> _visualObjects = new List<VisualObjectData>();
        readonly List<ObjectData> _objects = new List<ObjectData>();
        readonly ObjectDefinitions _defs;

        public LevelViewModel LevelViewModel
        {
            get { return _levelViewModel; }
        }

        public ObjectManager(LevelViewModel levelViewModel)
        {
            _levelViewModel = levelViewModel;
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
                _visualObjects.Remove(vod);
        }

        public VisualObjectData ParseObject(ObjectData obj)
        {
            var vod = new VisualObjectData();
            vod.ObjectData = obj;
            vod.Offset = new Vector3D(obj.Floats[0] / 4, obj.Floats[1] / 4, obj.Floats[2] / 4);
            vod.zRotation = 22.5 * (obj.I6 >> 12);

            vod = _defs.Parse(vod);

            if (vod != null)
            {
                _visualObjects.Add(vod);
            }

            return vod;
        }

        public VisualObjectData HitTest(ModelVisual3D hitResult)
        {
            foreach (var vod in _visualObjects)
            {
                if (HitTestModel(vod.Model, hitResult))
                    return vod;
            }
            return null;
        }

        private bool HitTestModel(ModelVisual3D obj, ModelVisual3D hitResult)
        {
            if (obj == hitResult)
                return true;
            foreach (var child in obj.Children)
            {
                if (child == hitResult)
                    return true;

                if (child is ModelVisual3D)
                {
                    if (HitTestModel((ModelVisual3D)child, hitResult))
                        return true;
                }
            }

            return false;
        }

        public ObjectData GetObjectByName(string name)
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
    }
}
