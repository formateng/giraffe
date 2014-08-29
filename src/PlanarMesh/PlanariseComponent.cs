using System;
using Grasshopper.Kernel;
using System.Drawing;
using PlanarMesh.WingedMeshSpace;
using System.Collections.Generic;
using Rhino.Geometry;
using PlanarMesh.PlanariseSpace;
using System.Linq;

namespace PlanarMesh
{
    public class PlanariseComponent : GH_Component
    {

        public PlanariseComponent()
            : base("Planarise mesh", "PM", "Re-mesh a mesh with planar panels", "RCD", "Remeshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("mesh", "m", "Mesh to planarise (as boundary curves)", GH_ParamAccess.list);
            //pManager.AddBooleanParameter("run", "R", "run planarisation", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("output", "out", "error messages from the system", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Proxies", "P", "the planes for the proxies", GH_ParamAccess.list);
            pManager.AddCurveParameter("MeshAsCurves", "MC", "the connectivity mesh as a set of curves", GH_ParamAccess.list); // TODO: replace with Plankton
            //pManager.Register_GenericParam("HLwingMesh", "WM", "the winged mesh to put into the next function");
            pManager.AddVectorParameter("Normals", "N", "the normals for each face", GH_ParamAccess.list);
            pManager.AddPointParameter("Centres", "C", "the centres for each face", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //container for errors/messages passed by controller, partition, etc.
            List<String> errorContainer = new List<String>();

            GH_PreviewUtil preview = new GH_PreviewUtil(true);

            //declare placeholder variables and assign initial empty mesh
            List<Curve> baseCurves = new List<Curve>();
            Boolean run = true; // TODO: remove

            //Retrieve input data
            if (!DA.GetDataList(0, baseCurves))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't infer list of polyline face boundaries from input");
                return;
            }
            //if (!DA.GetData(1, ref run)) { return; }

            List<Polyline> baseMesh = new List<Polyline>();
            for (int i = 0; i < baseCurves.Count; i++)
	        {
                 Polyline pl;
		         if (baseCurves[i].TryGetPolyline(out pl))
                 {
                     baseMesh.Add(pl);
                 }
                 else
                 {
                     this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't get a polyline from boundary curve #" + i.ToString());
                 }
	        }

            if (run)
            {
                try
                {
                    // TODO: disjoint mesh check

                    //create wingedmesh from rhinomesh
                    WingedMesh myMesh = new WingedMesh(errorContainer, baseMesh);
                    myMesh.calculateNormals();
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, myMesh.faces.Count.ToString() + " faces in wingMesh");
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, myMesh.vertices.Count.ToString() + " vertices in wingMesh");

                    PlanarMesher controller = new PlanarMesher(errorContainer, myMesh, null, myMesh.faces.Count, -1, preview);

                    controller.CreateFromInputMesh();
                    controller.createConnectivityMesh();
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
                    DA.SetDataList(0, proxyPlanes);
                    DA.SetDataList(1, boundaryEdges);
                    DA.SetDataList(2, faceNormals);
                    DA.SetDataList(3, faceCentres);
                }
                catch (Exception e)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.StackTrace);
                }
            }

            foreach (var item in errorContainer)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, item);
            }
        }

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("525BACC4-B753-43E6-A5FB-3554328F24FA");
            }
        }

        //setting icon image - currently just a random one at the moment!
        protected override Bitmap Icon
        {
           get
            {
                return PlanarMesh.Properties.Resources.planarMesh;
            }
        }
    }
}