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

namespace WorldExplorer
{
    /// <summary>
    /// A simple model that displays a YAK file.
    /// </summary>
    public class YakTreeViewModel : TreeViewItemViewModel
    {
        public YakTreeViewModel(TreeViewItemViewModel parent, YakFile yakFile) : base(parent, true)
        {
            _parent = parent;
            _yakFile = yakFile;
            _name = yakFile.Name;
        }

        private TreeViewItemViewModel _parent;
        private YakFile _yakFile;
        private string _name;

        public string Text
        {
            get { return _name; }
        }

        protected override void LoadChildren()
        {
            _yakFile.ReadEntries();
            var entries = _yakFile.Entries;
            int i = 0;
            foreach (var entry in entries)
            {
                Children.Add(new YakTreeViewItem(this, _yakFile, entry, "Entry "+i));
                ++i;
            }
        }
    }
}
