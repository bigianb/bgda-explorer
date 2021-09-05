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

namespace WorldExplorer.TreeView
{
    public class WorldTreeViewModel : TreeViewItemViewModel
    {
        private readonly World _world;

        public string WorldName => _world.Name;

        public WorldTreeViewModel(World world)
            : base(null, true)
        {
            _world = world;
        }

        public World World()
        {
            return _world;
        }

        protected override void LoadChildren()
        {
            _world.Load();

            if (_world.WorldLmp != null)
            {
                Children.Add(new LmpTreeViewModel(_world, this, _world.WorldLmp));
            }
            else if (_world.WorldGob != null)
            {
                Children.Add(new GobTreeViewModel(_world, this));
            }
            else if (_world.WorldYak != null)
            {
                Children.Add(new YakTreeViewModel(this, _world.WorldYak));
            }
            else if (_world.HdrDatFile != null)
            {
                Children.Add(new HdrDatTreeViewModel(this, _world.HdrDatFile));
            }
            else
            {
                throw new NotSupportedException("Unknown or corrupted file");
            }
        }
    }
}