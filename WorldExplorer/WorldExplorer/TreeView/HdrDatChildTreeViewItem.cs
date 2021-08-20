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
    public class HdrDatChildTreeViewItem : TreeViewItemViewModel
    {
        public HdrDatChildTreeViewItem(TreeViewItemViewModel parent, CacheFile cacheFile, CacheFile.Child value, CacheFile.Entry entry, string name)
            : base(parent, false)
        {
            _cacheFile = cacheFile;
            _value = value;
            _entry = entry;
            _name = name;
        }

        private CacheFile _cacheFile;
        private CacheFile.Child _value;
        private CacheFile.Entry _entry;
        private string _name;

        public CacheFile.Child Value => _value;

        public CacheFile.Entry ParentEntry => _entry;

        public CacheFile CacheFile => _cacheFile;

        public string Text => _name;
    }
}
