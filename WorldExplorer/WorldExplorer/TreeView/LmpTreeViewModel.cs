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
using WorldExplorer.DataLoaders;

namespace WorldExplorer
{
    /// <summary>
    /// A simple model that displays a LMP file.
    /// </summary>
    public class LmpTreeViewModel : AbstractLmpTreeViewModel
    {
        public LmpTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile)
            : base(world, parent, lmpFile, lmpFile.Name)
        {
        }

        protected override void LoadChildren()
        {
            _lmpFile.ReadDirectory();
            foreach (var entry in _lmpFile.Directory)
            {
                var ext = "";
                try
                {
                    ext = (System.IO.Path.GetExtension(entry.Key) ?? "").ToLower();
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                    continue;
                }

                TreeViewItemViewModel child;
                switch (ext)
                {
                    case ".world":
                        child = new WorldFileTreeViewModel(_world, this, _lmpFile, entry.Key);
                        break;
                    default:
                        child = new LmpEntryTreeViewModel(_world, this, _lmpFile, entry.Key);
                        break;
                }
                Children.Add(child);
            }
        }
    }
}
