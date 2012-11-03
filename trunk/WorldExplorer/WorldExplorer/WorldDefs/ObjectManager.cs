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

        public void ParseObject(ObjectData obj)
        {
            var vod = new VisualObjectData();
            vod.ObjectData = obj;
            vod.Offset = new Vector3D(obj.Floats[0] / 4, obj.Floats[1] / 4, obj.Floats[2] / 4);

            vod = _defs.Parse(vod);

            if (vod != null)
            {
                _visualObjects.Add(vod);

                vod.AddToScene(_levelViewModel);
            }
        }

        public VisualObjectData HitTest(ModelVisual3D hitResult)
        {
            return null;
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
