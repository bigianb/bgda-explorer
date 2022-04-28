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
    public class WorldFileTreeViewModel : AbstractLmpTreeViewModel
    {
        public WorldFileTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
            : base(world, parent, lmpFile, entryName)
        {
        }

        public void ReloadChildren()
        {
            Children.Clear();
            LoadChildren();
        }

        protected override void LoadChildren()
        {
            if (_world.WorldData == null)
            {
                // Force loading the tree item
                IsSelected = true;
                return; // Return to prevent adding elements twice
            }

            if (_world.WorldData != null)
            {
                foreach (var element in _world.WorldData.WorldElements)
                {
                    Children.Add(new WorldElementTreeViewModel(element, "Element " + element.ElementIndex + " 0x" + element.RawFlags.ToString("X4"), Parent, _world.WorldData));
                }
            }
        }
    }
}