using System;
using System.Collections.Generic;
using PlanarMesh.WingedMeshSpace;
using Rhino.Geometry;
using PlanarMesh.PlanariseSpace;
using Grasshopper.Kernel;

namespace PlanarMesh
{
    class PlanarMesher
    {
        //this class can be used to control and do the clustering and planarisation. The component will create an instance and then call methods.

        public List<String> errorContainer;
        public WingedMesh wingMesh;
        public Mesh rhinoMesh;
        public int numPanels;
        public Partition currentPartition;
        public GH_PreviewUtil preview;
        public int metricRef;

        public PlanarMesher(List<String> tErrorContainer, WingedMesh tWingMesh, Mesh tRhinoMesh, int tNumPanels, int metric, GH_PreviewUtil tPreview)
        {
            errorContainer = tErrorContainer;
            wingMesh = tWingMesh;
            rhinoMesh = tRhinoMesh;
            numPanels = tNumPanels;
            currentPartition = new Partition(wingMesh.faces.Count, this);
            preview = tPreview;
            metricRef = metric;
        }
        
        internal void createFirstCluster() {
            currentPartition.seedInitialProxies();
            currentPartition.startCluster();
            while (currentPartition.priorityQueue.Count > 0)
            {
                currentPartition.popOutTop();
            }
        }

        internal void iterateCluster()
        {
            currentPartition.updatePartitionAndProxies();
            currentPartition.startCluster();
            while (currentPartition.priorityQueue.Count > 0)
            {
                currentPartition.popOutTop();
            }
        }

        internal void createConnectivityMesh()
        {
            currentPartition.setVertexProxyConnectivity();  
            currentPartition.setProxyCornerVerts();
            currentPartition.sortProxyCornerVerts();
            currentPartition.convertProxiesToWingMesh();
        }

        /// <summary>
        /// Use this method to bypass the Lloyds clustering algorithm when the mesh to be planarised is already defined.
        /// </summary>
        internal void CreateFromInputMesh()
        {
            currentPartition.proxyToMesh = wingMesh;
            currentPartition.CreateProxiesFromWingedMesh();
        }

        internal void planariseConnectivityMesh()
        {
            currentPartition.runIntersections();//this runs, whilst checking and splitting any four vertices ones
            currentPartition.indentifyAllFlippedEdges();
            currentPartition.flipEdges();
            currentPartition.proxyToMesh.calculateNormals();
        }
    }
}