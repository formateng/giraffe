using Grasshopper;
using Grasshopper.Kernel;
using Giraffe.Properties;
using System;
using System.Drawing;

namespace Giraffe
{
    public class GiraffeInfo : GH_AssemblyInfo
    {
        public override string Name => "Giraffe";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Resources.Giraffe24x24;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "A tool for creating planar faces on input mesh with double curvature";

        public override Guid Id => new Guid("8267b9c1-8cc7-4284-b6cf-0d913d312ba8");

        //Return a string identifying you or your company.
        public override string AuthorName => "Format Engineers";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "info@formatengineers.com";

        //Set plugin version
        public override string Version => "0.1.2";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }

    public class GiraffeCategoryIcon : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("Giraffe", Resources.Giraffe16x16);
            Instances.ComponentServer.AddCategorySymbolName("Giraffe", 'G');
            return GH_LoadingInstruction.Proceed;
        }
    }
}
