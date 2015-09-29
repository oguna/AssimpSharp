using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new FbxFileTest("../../models-nonbsd/FBX/2013_ASCII/COLLADA.fbx");
            test.SetUp();
            test.TestAnimation();
            test.TestMeshes();
            test.TestMaterials();
            test.TestCameras();
            test.TestTextures();
            test.TestNodes();
            test.TestLights();
        }
    }
}
