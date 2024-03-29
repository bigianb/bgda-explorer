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

using JetBlackEngineLib.Data.Animation;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WorldExplorer;

internal class SkeletonProcessor
{
    private static readonly double delta = 0.1;

    public static Model3D? GetSkeletonModel(AnimData? animData, int frameNo)
    {
        if (null == animData)
        {
            return null;
        }

        GeometryModel3D model = new();
        MeshGeometry3D mesh = new();

        var parentPoints = new Point3D[64];
        parentPoints[0] = new Point3D(0, 0, 0);

        for (var jointNum = 0; jointNum < animData.SkeletonDef.GetLength(0); ++jointNum)
        {
            var parentIndex = animData.SkeletonDef[jointNum];
            // Binding position
            var pos = animData.BindingPose[jointNum];

            if (frameNo >= 0)
            {
                var pose = animData.PerFrameFkPoses?[frameNo, jointNum]
                           ?? throw new InvalidDataException("Invalid frame/bone pair encountered!");
                pos = pose.Position;
            }

            parentPoints[parentIndex + 1] = pos;
            AddBone(mesh, parentPoints[parentIndex], pos);
        }

        model.Geometry = mesh;

        DiffuseMaterial dm = new() {Brush = new SolidColorBrush(Colors.DarkGreen)};
        model.Material = dm;

        return model;
    }

    private static void AddBone(MeshGeometry3D mesh, Point3D startPoint, Point3D endPoint)
    {
        AxisAngleRotation3D rotate = new();
        RotateTransform3D xform = new(rotate);

        var boneVec = endPoint - startPoint;

        // radius always points towards -Z (when possible)
        Vector3D radius;

        if (boneVec.X == 0 && boneVec.Y == 0)
        {
            // Special case.
            radius = new Vector3D(0, -1, 0);
        }
        else
        {
            // Find vector axis 90 degrees from bone where Z == 0
            rotate.Axis = Vector3D.CrossProduct(boneVec, new Vector3D(0, 0, 1));
            rotate.Angle = -90;

            // Rotate 90 degrees to find radius vector
            radius = boneVec * xform.Value;
            radius.Normalize();
        }

        // Rotate the radius around the bone vector
        rotate.Axis = boneVec;

        var positions = mesh.Positions;
        const int slices = 10;
        for (var slice = 0; slice < slices; ++slice)
        {
            // Rotate radius vector 
            rotate.Angle = slice * 360.0 / slices;
            var vectRadius = radius * xform.Value;
            rotate.Angle = (slice + 1) * 360.0 / slices;
            var vectRadius1 = radius * xform.Value;

            // Bit of a hack to avoid having to set the normals or worry about consistent winding.
            positions.Add(startPoint);
            positions.Add(endPoint + (delta * vectRadius));
            positions.Add(endPoint + (delta * vectRadius1));

            positions.Add(startPoint);
            positions.Add(endPoint + (delta * vectRadius1));
            positions.Add(endPoint + (delta * vectRadius));
        }
    }
}