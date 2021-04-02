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
    public class YakChildTreeViewItem : TreeViewItemViewModel
    {
        public YakChildTreeViewItem(TreeViewItemViewModel parent, YakFile yakFile, YakFile.Child value, YakFile.Entry entry, string name)
            : base(parent, false)
        {
            _yakFile = yakFile;
            _value = value;
            _entry = entry;
            _name = name;
        }

        private YakFile _yakFile;
        private YakFile.Child _value;
        private YakFile.Entry _entry;
        private string _name;

        public YakFile.Child Value => _value;

        public YakFile.Entry ParentEntry => _entry;

        public YakFile YakFile => _yakFile;

        public string Text => _name;
    }
}
