using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssimpSharp.FBX;
using NUnit.Framework;
using System.IO;
using SharpDX;

namespace Test
{
    [TestFixture]
    public class DocumentTest
    {
        Document Document;

        [TestFixtureSetUp]
        public void LoadFile()
        {
            var file = "../../models-nonbsd/FBX/2013_ASCII/Cinema4D.fbx";
            byte[] input;
            using (var stream = new FileStream(file, FileMode.Open))
            {
                input = new byte[stream.Length];
                stream.Read(input, 0, (int)stream.Length);
            }
            bool isBinary = false;
            List<Token> tokens;
            if (Encoding.ASCII.GetString(input, 0, 18) == "Kaydara FBX Binary")
            {
                isBinary = true;
                BinaryTokenizer.TokenizeBinary(out tokens, input, input.Length);
            }
            else
            {
                Tokenizer.Tokenize(out tokens, input);
            }
            Parser parser = new Parser(tokens, isBinary);
            ImporterSettings settings = new ImporterSettings();
            this.Document = new Document(parser, settings);
        }

        [Test]
        public void DocumentTest_HeaderExtentions()
        {
            Assert.AreEqual(7300, Document.FBXVersion);
            Assert.AreEqual("FBX SDK/FBX Plugins version 2013.1", Document.Creator);
            Assert.AreEqual(new uint[] {2012, 6, 28, 16, 32, 53, 295 }, Document.CreationTimeStamp);
        }

        [Test]
        public void DocumentTest_GlobalSettings()
        {
            Assert.AreEqual("Producer Perspective", Document.GlobalSettings.DefaultCamera.Value);
        }

        [Test]
        public void DocumentTest_Connections()
        {
            var toRootNodeConns = this.Document.GetConnectionsByDestinationSequenced(0);
            Assert.AreEqual(40929088, toRootNodeConns[0].src);
            Assert.AreEqual(40742448, toRootNodeConns[1].src);
            Assert.AreEqual(40745360, toRootNodeConns[2].src);
        }

        [Test]
        public void DocumentTest_Materials()
        {
            var material = (Material)this.Document.Objects[40337536].Get();
            Assert.AreEqual("phong", material.ShadingModel);
            Assert.AreEqual(false, material.IsMultilayer);
            Assert.AreEqual(0.5, material.Props["ShininessExponent"].As<TypedProperty<float>>().Value);
            Assert.AreEqual("ID1", material.Props["COLLADA_ID"].As<TypedProperty<string>>().Value);
            Assert.AreEqual(new Vector3(0,0,0), material.Props["Emissive"].As<TypedProperty<Vector3>>().Value);
        }

        [Test]
        public void DocumentTest_Models()
        {
            var kegel = (Model)Document.Objects[40929088].Get();
            Assert.NotNull(kegel);
            Assert.AreEqual((Model.RotOrder)4, kegel.RotationOrder.Value);
            Assert.AreEqual(true, kegel.RotationActive.Value);
            Assert.AreEqual((Model.TransformInheritance)1, kegel.InheritType.Value);
            Assert.AreEqual(new Vector3(0, 0, 0), kegel.ScalingMax.Value);
            Assert.AreEqual("CullingOff", kegel.Culling);
            Assert.AreEqual("Y", kegel.Shading);
            var kegelMateril = kegel.Materials[0];
            Assert.AreEqual(40337536, kegelMateril.ID);
            var kegelGeometry = kegel.Geometry[0];
            Assert.AreEqual(40648208, kegelGeometry.ID);
        }

        [Test]
        public void DocumentTest_Geometries()
        {
            var id15 = (MeshGeometry)Document.Objects[40648208].Get();
            Assert.NotNull(id15);
            Assert.AreEqual("Geometry::ID5", id15.Name);
            //Assert.AreEqual(new Vector3(0, -100, 0) , id15.Vertices[0]);
            //Assert.AreEqual(13, id15.FaceIndexCounts[0]);
            //Assert.AreEqual(new Vector3(0, -1, 0), id15.Normals[0]);
        }

        [Test]
        public void DocumentTest_Takes()
        {
            Assert.AreEqual(1, Document.AnimationStacks.Count);
            var take = Document.AnimationStacks[0];
            Assert.AreEqual("AnimStack::Take 001", take.Name);
            var layer = take.Layers[0];
            Assert.AreEqual(40506352, layer.ID);
        }


    }
}
