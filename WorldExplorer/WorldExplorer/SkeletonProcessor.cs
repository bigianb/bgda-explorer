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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using WorldExplorer.DataLoaders;
using System.Windows.Media;

namespace WorldExplorer
{
    class SkeletonProcessor
    {
        public static Model3D GetSkeletonModel(AnimData animData)
        {
            GeometryModel3D model = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();

            Point3D[] parentPoints = new Point3D[64];
            parentPoints[0] = new Point3D(0, 0, 0);

            for (int jointNum = 0; jointNum < animData.skeletonDef.GetLength(0); ++jointNum)
            {
                int parentIndex = animData.skeletonDef[jointNum];
                var headPos = animData.jointPositions[jointNum];

                parentPoints[parentIndex + 1] = headPos;
                AddBone(mesh, parentPoints[parentIndex], headPos);
            }

            model.Geometry = mesh;

            DiffuseMaterial dm = new DiffuseMaterial();
            dm.Brush = new SolidColorBrush(Colors.DarkGreen);
            model.Material = dm;

            return model;
        }

        private static double delta = 0.1;

        private static void AddBone(MeshGeometry3D mesh, Point3D startPoint, Point3D endPoint)
        {
            AxisAngleRotation3D rotate = new AxisAngleRotation3D();
            RotateTransform3D xform = new RotateTransform3D(rotate);

            Vector3D boneVec = endPoint - startPoint;

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
            for (int slice = 0; slice < slices; ++slice)
            {
                // Rotate radius vector 
                rotate.Angle = slice * 360.0 / slices;
                Vector3D vectRadius = radius * xform.Value;
                rotate.Angle = (slice + 1) * 360.0 / slices;
                Vector3D vectRadius1 = radius * xform.Value;

                // Bit of a hack to avoid having to set the normals or worry about consistent winding.
                positions.Add(startPoint);
                positions.Add(endPoint + delta * vectRadius);             
                positions.Add(endPoint + delta * vectRadius1);

                positions.Add(startPoint);
                positions.Add(endPoint + delta * vectRadius1);
                positions.Add(endPoint + delta * vectRadius);
            }

        }

    }
}
