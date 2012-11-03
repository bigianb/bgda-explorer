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
using WorldExplorer.DataLoaders;
using WorldExplorer.DataModel;

namespace WorldExplorer
{
    public class WorldTreeViewModel : TreeViewItemViewModel
    {
        readonly World _world;

        public WorldTreeViewModel(World world) 
            : base(null, true)
        {
            _world = world;
        }

        public World World()
        {
            return _world;
        }

        public string WorldName
        {
            get { return _world.Name; }
        }

        protected override void LoadChildren()
        {
            _world.Load();

            if (_world.WorldLmp != null)
            {
                base.Children.Add(new LmpTreeViewModel(_world, this, _world.WorldLmp));
            }
            else if (_world.WorldGob != null)
            {
                base.Children.Add(new GobTreeViewModel(_world, this));
            }
            else
            {
                throw new NotSupportedException("Unknown or corrupted file");
            }
        }
    }

    /// <summary>
    /// A simple model that just displays a text label.
    /// </summary>
    public class TextTreeViewModel : TreeViewItemViewModel
    {
        public TextTreeViewModel(World world, TreeViewItemViewModel parent, string text) : base (parent, false)
        {
            _text = text;
        }

        private string _text;

        public string Text
        {
            get { return _text; }
        }
    }

    /// <summary>
    /// A simple model that displays a GOB file.
    /// </summary>
    public class GobTreeViewModel : TreeViewItemViewModel
    {
        public GobTreeViewModel(World world, TreeViewItemViewModel parent)
            : base(parent, true)
        {
            _world = world;
        }

        private World _world;

        public string Text
        {
            get { return _world.WorldGob.Filename; }
        }

        protected override void LoadChildren()
        {
            foreach (var entry in _world.WorldGob.Directory)
            {
                Children.Add(new LmpTreeViewModel(_world, this, entry.Value));
            }
        }
    }

    public abstract class AbstractLmpTreeViewModel : TreeViewItemViewModel
    {
        public AbstractLmpTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
            : base(parent, true)
        {
            _lmpFile = lmpFile;
            _name = entryName;
            _world = world;
        }

        protected LmpFile _lmpFile;
        protected string _name;
        protected World _world;

        public LmpFile LmpFileProperty
        {
            get { return _lmpFile; }
        }

        public string Text
        {
            get { return _name; }
        }
    }

    /// <summary>
    /// A simple model that displays a LMP file.
    /// </summary>
    public class LmpTreeViewModel : AbstractLmpTreeViewModel
    {
        public LmpTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile)
            : base(world, parent, lmpFile, lmpFile.Name)
        {
        }

        protected override void LoadChildren()
        {
            _lmpFile.ReadDirectory();
            foreach (var entry in _lmpFile.Directory)
            {
                var ext = (System.IO.Path.GetExtension(entry.Key) ?? "").ToLower();

                TreeViewItemViewModel child;
                switch (ext)
                {
                    case ".world":
                        child = new WorldFileTreeViewModel(_world, this, _lmpFile, entry.Key);
                        break;
                    default:
                        child = new LmpEntryTreeViewModel(_world, this, _lmpFile, entry.Key);
                        break;
                }
                Children.Add(child);
            }
        }
    }

    /// <summary>
    /// A simple model that displays an entry in a LMP file.
    /// </summary>
    public class LmpEntryTreeViewModel : AbstractLmpTreeViewModel
    {
        public LmpEntryTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
            : base(world, parent, lmpFile, entryName)
        {
        }

        protected override void LoadChildren()
        {
            
        }
    }

    public class WorldFileTreeViewModel : AbstractLmpTreeViewModel
    {
        public WorldFileTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
            : base(world, parent, lmpFile, entryName)
        {
        }

        public void ReloadChildren()
        {
            Children.Clear();
            LoadChildren();
        }

        protected override void LoadChildren()
        {
            if (_world.worldData == null)
            {
                // Force loading the tree item
                this.IsSelected = true;
                return; // Return to prevent adding elements twice
            }
            if (_world.worldData != null){
                int i = 0;
                foreach (var element in _world.worldData.worldElements)
                {
                    Children.Add(new WorldElementTreeViewModel(element, Parent, "Element " + i));
                    ++i;
                }
            }
        }
    }

    public class WorldElementTreeViewModel : TreeViewItemViewModel
    {
        public WorldElementTreeViewModel(WorldElement worldElement, TreeViewItemViewModel parent, string name)
            : base(parent, true)
        {
            _worldElement = worldElement;
            _name = name;
        }

        private string _name;
        private WorldElement _worldElement;

        public WorldElement WorldElement
        { get { return _worldElement; } }

        public string Text
        {
            get { return _name; }
        }

        protected override void LoadChildren()
        {
            
        }
    }
}
