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

using WorldExplorer.DataLoaders;

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

        public string WorldName
        {
            get { return _world.Name; }
        }

        protected override void LoadChildren()
        {
            _world.Load();
            base.Children.Add(new GobTreeViewModel(this, _world.WorldGob));
        }
    }

    /// <summary>
    /// A simple model that just displays a text label.
    /// </summary>
    public class TextTreeViewModel : TreeViewItemViewModel
    {
        public TextTreeViewModel(TreeViewItemViewModel parent, string text) : base (parent, false)
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
        public GobTreeViewModel(TreeViewItemViewModel parent, GobFile gobFile)
            : base(parent, true)
        {
            _gobFile = gobFile;
        }

        private GobFile _gobFile;

        public string Text
        {
            get { return _gobFile.Filename; }
        }

        protected override void LoadChildren()
        {
            foreach (var entry in _gobFile.Directory) {
                base.Children.Add(new LmpTreeViewModel(this, entry.Value));
            }
        }
    }

    /// <summary>
    /// A simple model that displays a LMP file.
    /// </summary>
    public class LmpTreeViewModel : TreeViewItemViewModel
    {
        public LmpTreeViewModel(TreeViewItemViewModel parent, LmpFile lmpFile)
            : base(parent, true)
        {
            _lmpFile = lmpFile;
        }

        private LmpFile _lmpFile;

        public string Text
        {
            get { return _lmpFile.Name; }
        }

        protected override void LoadChildren()
        {
            _lmpFile.ReadDirectory();
            foreach (var entry in _lmpFile.Directory) {
                var child = new TextTreeViewModel(this, entry.Key);
                base.Children.Add(child);
            }
        }
    }
}
