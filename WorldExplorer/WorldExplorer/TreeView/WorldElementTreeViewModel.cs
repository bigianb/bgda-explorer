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

using WorldExplorer.DataModel;

namespace WorldExplorer
{
    public class WorldElementTreeViewModel : TreeViewItemViewModel
    {
        public WorldData WorldData { get; }
        public WorldElement WorldElement { get; }

        public string Label { get; }

        public WorldElementTreeViewModel(WorldElement worldElement, string label, TreeViewItemViewModel? parent,
            WorldData worldData)
            : base(parent, true)
        {
            WorldData = worldData;
            WorldElement = worldElement;
            Label = label;
        }
        
        protected override void LoadChildren()
        {
        }
    }
}