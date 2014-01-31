using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PlanarMesh.WingedMeshSpace;
using Rhino.Geometry;


namespace PlanarMeshTest
{
    [TestFixture]
    public class Class1
    {
     
        Vertex SUT;

        [SetUp]
        public void Setup()
        {
            SUT = new Vertex(0, new Vector3f(0.0f,10.0f,0.0f), new Vector3f(0.0f,5.0f,0.0f));
        }

        [Test]
        public void CanDoSimpleTest()
        {
            Assert.AreEqual(4, 2 * 2);
        }

        [TearDown]
        public void TearDown()
        {
            SUT = null;
        }
    }
}
