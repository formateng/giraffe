using System;
using Grasshopper.Kernel;
using System.Drawing;
using Giraffe.WingedMeshSpace;
using System.Collections.Generic;
using Rhino.Geometry;
using Giraffe.PlanariseSpace;

namespace Giraffe
{
    public class Component_OBSOLETE : GH_Component
    {

        public Component_OBSOLETE()
            : base("Planarise mesh", "PM", "Re-mesh a mesh with planar panels", "Giraffe", "Planar Remeshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //change the param access thing to have individual items, list or tree etc
            pManager.Register_MeshParam("mesh", "m", "Mesh to planarise");

            pManager.Register_IntegerParam("metric", "D/N", " 0 = euclidian error metric, 1 = normal based error metric", 1);

            pManager.Register_IntegerParam("numberOfPanels", "n", "number of planar panels required for output", 90);

            pManager.Register_BooleanParam("run", "R", "run planarisation", false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_StringParam("output", "out", "error messages from the system");

            pManager.Register_PlaneParam("Proxies", "P", "the planes for the proxies");

            pManager.Register_CurveParam("MeshAsCurves", "MC", "the connectivity mesh as a set of curves");

            pManager.Register_GenericParam("HLwingMesh", "WM", "the winged mesh to put into the next function");

            pManager.Register_VectorParam("Normals", "N", "the normals for each face");

            pManager.Register_PointParam("Centres", "C", "the centres for each face");

            pManager.Register_CurveParam("VoronoiOnMesh", "VOM", "non planar voronoi from clusters");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //container for errors/messages
            List<String> errorContainer = new List<String>();

            GH_PreviewUtil preview = new GH_PreviewUtil(true);

            //declare placeholder variables and assign initial empty mesh
            Mesh baseMesh = new Rhino.Geometry.Mesh();
            int errorMetricIdentifer = -1;
            int numPanels = -1;
            Boolean run = false;

            //Retrieve input data
            if (!DA.GetData(0, ref baseMesh)) { return; }
            if (!DA.GetData(1, ref errorMetricIdentifer)) { return; }
            if (!DA.GetData(2, ref numPanels)) { return; }
            if (!DA.GetData(3, ref run)) { return; }

            if (run)
            {

                if (baseMesh.DisjointMeshCount > 1)
                {
                    errorContainer.Add("Problem with mesh input - disjoint mesh");
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
                        controller.currentPartition.drawProxies(preview);
                    }

                    controller.createConnectivityMesh();

                    //creating voronoi
                    WingedMesh voronoiMesh = new WingedMesh(errorContainer, controller.currentPartition.proxyToMesh.convertWingedMeshToPolylines());

                    controller.planariseConnectivityMesh();

                    //convert faces edges to polylines for viewing
                    List<Polyline> boundaryEdges = controller.currentPartition.proxyToMesh.convertWingedMeshToPolylines();

                    List<Plane> proxyPlanes = new List<Plane>();
                    foreach (Proxy proxy in controller.currentPartition.proxies)
                    {
                        proxyPlanes.Add(proxy.rhinoPlane);
                    }
                    List<Mesh> proxyMeshes = new List<Mesh>();
                    foreach (Proxy proxy in controller.currentPartition.proxies)
                    {
                        proxyMeshes.Add(proxy.proxyAsMesh);
                    }

                    List<Vector3d> faceNormals = new List<Vector3d>();
                    List<Point3d> faceCentres = new List<Point3d>();
                    for (int i = 0; i < controller.currentPartition.proxyToMesh.faces.Count; i++)
                    {
                        faceNormals.Add(new Vector3d(controller.currentPartition.proxyToMesh.faces[i].faceNormal));
                        faceCentres.Add(new Point3d(controller.currentPartition.proxyToMesh.faces[i].faceCentre));
                    }

                    //set all the output data
                    DA.SetDataList(1, proxyPlanes);
                    DA.SetDataList(2, boundaryEdges);
                    DA.SetData(3, controller.currentPartition.proxyToMesh);
                    DA.SetDataList(4, faceNormals);
                    DA.SetDataList(5, faceCentres);
                    DA.SetDataList(6, voronoiMesh.convertWingedMeshToPolylines());
                }
                DA.SetDataList(0, errorContainer);
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("CADFBB1C-218E-47a7-AF52-369D0448958E");
            }
        }

        //setting icon image - currently just a random one at the moment!
        protected override Bitmap Icon
        {
           get
            {
                return Giraffe.Properties.Resources.planarMesh;
            }
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.hidden;
            }
        }
    }
}