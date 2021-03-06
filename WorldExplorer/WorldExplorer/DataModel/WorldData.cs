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
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace WorldExplorer.DataModel
{
    public class WorldData
    {
        public List<WorldElement> worldElements;
        public int[,] textureChunkOffsets;
    }

    public class WorldElement
    {
        public Rect3D boundingBox;
        public Model model;
        public WriteableBitmap Texture;

        // The position before rotation
        public Vector3D pos;

        public bool usesRotFlags;
        public int xyzRotFlags;

        public double cosAlpha;
        public double sinAlpha;
        // Whether we should flip the y axis (when not using rot flags)
        public bool negYaxis;

        // Store info to access again
        public int VifDataOffset;
        public int VifDataLength;
    }
}
