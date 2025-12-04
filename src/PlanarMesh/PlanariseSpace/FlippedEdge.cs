using System;
using Rhino.Geometry;
using Giraffe.WingedMeshSpace;

namespace Giraffe.PlanariseSpace
{
    class FlippedEdge
    {
        public Edge edge;

        public Point3d rightFaceIntersection;
        public Point3d leftFaceIntersection;

        public Boolean doubleIntersection;

        public FlippedEdge(Edge tFlippedEdge, Face tFirstFace, Point3d tFirstFaceIntersection)
        {
            edge = tFlippedEdge;
            if (edge.rightFace == tFirstFace)
            {
                rightFaceIntersection = tFirstFaceIntersection;
            }
            else
            {
                leftFaceIntersection = tFirstFaceIntersection;
            }
            doubleIntersection = false;
        }

        public void addSecondInfo(Face tSecondFace, Point3d tSecondFaceIntersection) {
            if (edge.rightFace == tSecondFace)
            {
                rightFaceIntersection = tSecondFaceIntersection;
            }
            else
            {
                leftFaceIntersection = tSecondFaceIntersection;
            }
            doubleIntersection = true;
        }
    }
}
