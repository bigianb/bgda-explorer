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

namespace WorldExplorer.TreeView;

/// <summary>
/// A simple model that displays a GOB file.
/// </summary>
public class GobTreeViewModel : TreeViewItemViewModel
{
    private readonly World _world;

    public GobTreeViewModel(World world, TreeViewItemViewModel parent)
        : base(world.WorldGob?.Name ?? "[ERROR: NO GOB FILE]", parent, true)
    {
        _world = world;
    }

    protected override void LoadChildren()
    {
        if (_world.WorldGob == null) return;
        foreach (var entry in _world.WorldGob.Directory)
        {
            Children.Add(new LmpTreeViewModel(_world, this, entry.Value));
        }
    }
}