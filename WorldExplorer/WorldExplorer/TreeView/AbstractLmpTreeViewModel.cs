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

using WorldExplorer.DataLoaders;

namespace WorldExplorer
{
    public abstract class AbstractLmpTreeViewModel : TreeViewItemViewModel
    {
        public AbstractLmpTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
            : base(parent, true)
        {
            _lmpFile = lmpFile;
            _name = entryName;
            _world = world;
        }

        protected LmpFile _lmpFile;
        protected string _name;
        protected World _world;

        public LmpFile LmpFileProperty => _lmpFile;

        public string Text => _name;
    }
}
