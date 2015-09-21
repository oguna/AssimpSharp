using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Diagnostics;

namespace AssimpSharp.FBX
{
    [TestFixture]
    public class SpeedComparisonWithAssimpNet
    {

        void LoadAssimpSharpScene(string file)
        {
            var assimpSharpImporter = new FBXImporter();
            var assimpSharpScene = assimpSharpImporter.ReadFile(file);
        }

        void LoadAssimpNetScene(string file)
        {
            var importer = new Assimp.AssimpContext();
            var assimpNetScene = importer.ImportFile(file);
        }

        [TestCase("../../../models_nonbsd/2013_ASCII/Cinema4D.fbx")]
        public void SpeedComparison(string file)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            LoadAssimpNetScene(file);
            stopwatch.Stop();
            var netTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            LoadAssimpSharpScene(file);
            stopwatch.Stop();
            var sharpTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine("AssimpNet : " + netTime);
            Console.WriteLine("AssimpSharp : " + sharpTime);
            Assert.LessOrEqual(netTime, sharpTime);
        }
    }
}
