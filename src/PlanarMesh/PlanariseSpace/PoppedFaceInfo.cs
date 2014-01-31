using System;

namespace PlanarMesh.PlanariseSpace
{
    class PoppedFaceInfo//definitely should be a class so if is passed by value not reference!
    {
        public double error;
        public int faceIndex;
        public int proxyIndex;

        public PoppedFaceInfo(double tError, int tFaceIndex, int tProxyIndex)
        {
            error = tError;
            faceIndex = tFaceIndex;
            proxyIndex = tProxyIndex;
        }
    }
}
