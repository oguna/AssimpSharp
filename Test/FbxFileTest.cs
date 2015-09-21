using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Test
{
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/Cinema4D.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/COLLADA.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/ConcavePolygon.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/cube_with_2UVs.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/duck.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/Granate.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/jeep1.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/kwxport_test_vcolors.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/mar_rifle.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/mp5_sil.fbx")]
    [TestFixture("../../models_nonbsd/FBX/2013_ASCII/pyramob.fbx")]
    [Category("FBX")]
    public class FbxFileTest : FileTest
    {
        public FbxFileTest(string file) : base(file)
        { }

        [TestFixtureSetUp]
        public void SetUp()
        {
            var assimpNetimporter = new Assimp.AssimpContext();
            Assimp.LogStream.IsVerboseLoggingEnabled = true;
            var logger = new Assimp.ConsoleLogStream();
            logger.Attach();
            assimpNetScene = assimpNetimporter.ImportFile(filename);
            logger.Detach();
            var assimpSharpImporter = new AssimpSharp.FBX.FBXImporter();
            assimpSharpScene = assimpSharpImporter.ReadFile(filename);
        }
    }
}
