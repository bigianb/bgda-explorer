/*  Copyright (C) 2021 Ian Brown

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
    public class HdrDatTreeViewItem : TreeViewItemViewModel
    {
        private readonly CacheFile _cacheFile;
        private readonly CacheFile.Entry _entry;

        public string Text { get; }

        public HdrDatTreeViewItem(TreeViewItemViewModel parent, CacheFile cacheFile, CacheFile.Entry entry, string name)
            : base(parent, true)
        {
            _cacheFile = cacheFile;
            _entry = entry;
            Text = name;
        }

        protected override void LoadChildren()
        {
            var i = 0;
            foreach (var child in _entry.children)
            {
                Children.Add(new HdrDatChildTreeViewItem(this, _cacheFile, child, _entry, "Id " + child.id));
                ++i;
            }
        }
    }
}