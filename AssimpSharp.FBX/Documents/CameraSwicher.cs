using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM base class for FBX camera settings attached to a node
    /// </summary>
    public class CameraSwicher : NodeAttribute
    {
        public CameraSwicher(ulong id, Element element, Document doc, string name)
            : base(id, element, doc, name)
        {
            var sc = Parser.GetRequiredScope(element);
            var CameraId = sc["CameraId"];
            var CameraName = sc["CameraName"];
            var CameraIndexName = sc["CameraIndexName"];
            if (CameraId != null)
            {
                cameraId = Parser.ParseTokenAsInt(Parser.GetRequiredToken(CameraId, 0));
            }
            if (CameraName != null)
            {
                cameraName = Parser.GetRequiredToken(CameraName, 0).StringContents;
            }
            if (CameraIndexName != null && CameraIndexName.Tokens.Count > 0)
            {
                cameraIndexName = Parser.GetRequiredToken(CameraIndexName, 0).StringContents;
            }
        }

        private int cameraId;
        private string cameraName;
        private string cameraIndexName;

        public int CameraId
        {
            get
            {
                return cameraId;
            }
        }

        public string CameraName
        {
            get
            {
                return cameraName;
            }
        }

        public string CameraIndexName
        {
            get
            {
                return cameraIndexName;
            }
        }
    }
}
