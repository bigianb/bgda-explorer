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

using JetBlackEngineLib.Data.DataContainers;

namespace WorldExplorer.TreeView;

public class HdrDatChildTreeViewItem : TreeViewItemViewModel
{
    public CacheFile.Child Value { get; }

    public CacheFile.Entry ParentEntry { get; }

    public CacheFile CacheFile { get; }

    public HdrDatChildTreeViewItem(TreeViewItemViewModel parent, CacheFile cacheFile, CacheFile.Child value,
        CacheFile.Entry entry, string name)
        : base(name, parent, false)
    {
        CacheFile = cacheFile;
        Value = value;
        ParentEntry = entry;
    }
}