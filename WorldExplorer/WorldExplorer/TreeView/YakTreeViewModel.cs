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
    /// <summary>
    /// A simple model that displays a YAK file.
    /// </summary>
    public class YakTreeViewModel : TreeViewItemViewModel
    {
        private readonly YakFile _yakFile;
        private TreeViewItemViewModel _parent;

        public string Text { get; }

        public YakTreeViewModel(TreeViewItemViewModel parent, YakFile yakFile) : base(parent, true)
        {
            _parent = parent;
            _yakFile = yakFile;
            Text = yakFile.Name;
        }

        protected override void LoadChildren()
        {
            _yakFile.ReadEntries();
            var entries = _yakFile.Entries;
            var i = 0;
            foreach (var entry in entries)
            {
                Children.Add(new YakTreeViewItem(this, _yakFile, entry, "Entry " + i));
                ++i;
            }
        }
    }
}