using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NUnit.Framework;
using System.Diagnostics;

namespace Test
{
    public class SpeedTest
    {

        void LoadAssimpSharpScene(string file)
        {
            var assimpSharpImporter = new AssimpSharp.FBX.FBXImporter();
            var assimpSharpScene = assimpSharpImporter.ReadFile(file);
        }

        void LoadAssimpNetScene(string file)
        {
            var importer = new Assimp.AssimpContext();
            var assimpNetScene = importer.ImportFile(file);
        }

        [TestCase("../../models-nonbsd/FBX/2013_ASCII/Cinema4D.fbx")]
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
        
        public void TestXFiles()
        {
            var dir = "../../models/x";
            var files = new[] { "anim_test", "BCN_Epileptic", "fromtruespace_bin32", "kwxport_test_cubewithvcolors", "test", "test_cube_binary", "test_cube_compressed", "test_cube_text", "Testwuson" };
            Console.WriteLine("Format |  Net  | Sharp | Filename");
            Console.WriteLine("----+-------+-------+-------------------");
            foreach(var file in files)
            {
                TestXFile(file + ".x", dir);
            }
        }

        void TestXFile(string file, string dir)
        {
            var path = Path.Combine(dir, file);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            LoadAssimpNetScene(path);
            stopwatch.Stop();
            var netTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            var assimpSharpImporter = new AssimpSharp.XFile.XFileImporter();
            var assimpSharpScene = new AssimpSharp.Scene();
            assimpSharpImporter.InternReadFile(path, assimpSharpScene);
            stopwatch.Stop();
            var sharpTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine(" {0,4}  | {1,5} | {2,5} | {3}", "x", netTime, sharpTime, file);
        }
    }
}
