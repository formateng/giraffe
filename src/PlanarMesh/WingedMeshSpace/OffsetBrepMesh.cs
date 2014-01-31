using System;
using System.Collections.Generic;
using System.Text;
using Rhino.Geometry;

namespace PlanarMesh.WingedMeshSpace
{
    // the collection of breps that corresponds to the wingedMesh
    class OffsetBrepMesh
    {
        float thickness;
        float minimumDistance;
        float hingeWidth;
        float hingeDepth;
        public List<float> offsetEdgeValues;
        public Polyline[] offsetEdges;
        public Brep[] offsetSolids;
        WingedMesh refMesh;

        public OffsetBrepMesh(WingedMesh tRefMesh, float tThickness, float tMinimumDistance, float tHingeWidth, float tHingeDepth)
        {
            refMesh = tRefMesh;
            thickness = tThickness;
            minimumDistance = tMinimumDistance;
            hingeWidth = tHingeWidth;
            hingeDepth = tHingeDepth;

            offsetEdgeValues = calculateOffsetEdgeValues();
            offsetEdges = calculateOffsetEdges();
            offsetSolids = calculateOffsetSolids();
        }

        private List<float> calculateOffsetEdgeValues()
        {
            List<float> edgeOffsetValues = new List<float>();
            for (int i = 0; i < refMesh.edges.Count; i++)
            {
                if (!refMesh.edges[i].boundaryEdge)
                {
                    edgeOffsetValues.Add(refMesh.edges[i].calculateThicknessOffset(thickness, minimumDistance));
                }
                else
                {//if on a boundary say no offset. we may in fact want to add a standard offset of something
                    edgeOffsetValues.Add(0f);
                }
            }
            return edgeOffsetValues;
        }

        private Polyline[] calculateOffsetEdges()
        {
            Polyline[] insetLines = new Polyline[refMesh.faces.Count];
            for (int i = 0; i < refMesh.faces.Count; i++)
            {
                insetLines[i] = refMesh.faces[i].convertFaceToOffsetPolyline(offsetEdgeValues);
            }
            return insetLines;
        }

        private Brep[] calculateOffsetSolids()
        {
            Brep[] cappedBreps = new Brep[refMesh.faces.Count];
            for (int i = 0; i < offsetEdges.Length; i++)
            {
                List<Brep> brepsToMerge = new List<Brep>();
                refMesh.faces[i].faceNormal.Unitize();
                //better to translate and then just create on.

                Transform moveToAdjustForThickness = Transform.Translation(Vector3d.Multiply(thickness/2,refMesh.faces[i].faceNormal));

                offsetEdges[i].ToNurbsCurve().Transform(moveToAdjustForThickness);

                cappedBreps[i] = Surface.CreateExtrusion(offsetEdges[i].ToNurbsCurve(), Vector3d.Multiply(-thickness, refMesh.faces[i].faceNormal)).ToBrep().CapPlanarHoles(0.1);

            }
            return cappedBreps;
        }

        private List<Polyline>[] hingeBreps()
        {
            //maybe also 
            List<Polyline>[] hingeOutline = new List<Polyline>[refMesh.faces.Count];

            //go through each vertex and calculate the minimum distance from the edge.
            //if over half way then now possible...

            for (int i = 0; i < refMesh.vertices.Count; i++)
            {
                //go through and calculate the min 
            }
            return hingeOutline;
        }
    }
}
