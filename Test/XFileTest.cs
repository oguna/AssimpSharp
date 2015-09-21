using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Test
{
    [TestFixture("../../models/x/test.x")]
    [Category("XFile")]
    public class XFileTest : FileTest
    {
        public XFileTest(string filename) : base(filename)
        {
        }

        [TestFixtureSetUp]
        public void SetUp()
        {
            var assimpNetimporter = new Assimp.AssimpContext();
            Assimp.LogStream.IsVerboseLoggingEnabled = true;
            var logger = new Assimp.ConsoleLogStream();
            logger.Attach();
            assimpNetScene = assimpNetimporter.ImportFile(filename);
            logger.Detach();
            var assimpSharpImporter = new AssimpSharp.XFile.XFileImporter();
            assimpSharpScene = new AssimpSharp.Scene();
            assimpSharpImporter.InternReadFile(filename, assimpSharpScene);
        }
    }
}
