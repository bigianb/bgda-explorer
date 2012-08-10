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
            // Bit of a hack
            Point3D ep1 = new Point3D(endPoint.X - delta, endPoint.Y - delta, endPoint.Z);
            Point3D ep2 = new Point3D(endPoint.X - delta, endPoint.Y + delta, endPoint.Z);
            Point3D ep3 = new Point3D(endPoint.X + delta, endPoint.Y + delta, endPoint.Z);
            Point3D ep4 = new Point3D(endPoint.X + delta, endPoint.Y - delta, endPoint.Z);

            var positions = mesh.Positions;

            positions.Add(startPoint);
            positions.Add(ep1);
            positions.Add(ep2);

            positions.Add(startPoint);
            positions.Add(ep2);
            positions.Add(ep3);

            positions.Add(startPoint);
            positions.Add(ep3);
            positions.Add(ep4);

            positions.Add(startPoint);
            positions.Add(ep4);
            positions.Add(ep1);

            positions.Add(ep3);
            positions.Add(ep2);
            positions.Add(ep1);

            positions.Add(ep1);
            positions.Add(ep4);
            positions.Add(ep3);
        }
    }
}
