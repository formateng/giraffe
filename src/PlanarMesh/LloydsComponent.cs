using System;
using Grasshopper.Kernel;
using System.Drawing;
using Giraffe.WingedMeshSpace;
using System.Collections.Generic;
using Rhino.Geometry;
using Giraffe.PlanariseSpace;

namespace Giraffe
{
    public class LloydsComponent : GH_Component
    {

        public LloydsComponent()
            : base("Lloyds", "Ll", "Lloyds clustering algorithm", "Giraffe", "Remeshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "m", "Mesh to perform clustering on", GH_ParamAccess.item);
            pManager.AddIntegerParameter("metric", "D/N", " 0 = euclidian error metric, 1 = normal based error metric", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("numberOfFacets", "n", "number of facets required for output", GH_ParamAccess.item, 90);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("MeshAsCurves", "MC", "the connectivity mesh as a set of curves", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //container for errors/messages passed by controller, partition, etc.
            List<String> errorContainer = new List<String>();

            GH_PreviewUtil preview = new GH_PreviewUtil(true);

            //declare placeholder variables and assign initial empty mesh
            Mesh baseMesh = new Rhino.Geometry.Mesh();
            int errorMetricIdentifer = -1;
            int numPanels = -1;

            //Retrieve input data
            if (!DA.GetData(0, ref baseMesh)) { return; }
            if (!DA.GetData(1, ref errorMetricIdentifer)) { return; }
            if (!DA.GetData(2, ref numPanels)) { return; }

            if (baseMesh.DisjointMeshCount > 1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Problem with mesh input - disjoint mesh");
            }
            else
            {
                //compute and unify normal
                baseMesh.Normals.ComputeNormals();
                baseMesh.UnifyNormals();

                //create wingedmesh from rhinomesh
                WingedMesh myMesh = new WingedMesh(errorContainer, baseMesh);

                PlanarMesher controller = new PlanarMesher(errorContainer, myMesh, baseMesh, numPanels, errorMetricIdentifer, preview);

                controller.createFirstCluster();

                for (int i = 0; i < 40; i++)
                {
                    controller.iterateCluster();
                    //controller.currentPartition.drawProxies(preview);
                }

                controller.createConnectivityMesh();

                //creating voronoi
                WingedMesh voronoiMesh = new WingedMesh(errorContainer, controller.currentPartition.proxyToMesh.convertWingedMeshToPolylines());

                //set all the output data
                DA.SetDataList(0, voronoiMesh.convertWingedMeshToPolylines());
            }

            foreach (var item in errorContainer)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, item);
            }
        }

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("CFED400F-5D13-4CD2-8340-EEEF965B32B9");
            }
        }

        //setting icon image - currently just a random one at the moment!
        protected override Bitmap Icon
        {
            get
            {
                return Giraffe.Properties.Resources.lloydsMesh;
            }
        }
    }
}