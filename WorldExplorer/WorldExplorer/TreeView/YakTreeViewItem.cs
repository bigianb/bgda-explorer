﻿/*  Copyright (C) 2012 Ian Brown

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

using JetBlackEngineLib.Data.Textures;

namespace WorldExplorer.TreeView;

public class YakTreeViewItem : TreeViewItemViewModel
{
    private readonly YakEntry _entry;

    private readonly YakFile _yakFile;

    public YakTreeViewItem(TreeViewItemViewModel parent, YakFile yakFile, YakEntry entry, string name)
        : base(name, parent, true)
    {
        _yakFile = yakFile;
        _entry = entry;
    }

    protected override void LoadChildren()
    {
        var i = 0;
        foreach (var child in _entry.Children)
        {
            Children.Add(new YakChildTreeViewItem(this, _yakFile, child, _entry, "Child " + i));
            ++i;
        }
    }
}