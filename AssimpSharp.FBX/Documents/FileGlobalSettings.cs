using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// DOM class for global document settings, a single instance per document can
    /// be accessed via Document.Globals().
    /// </summary>
    public class FileGlobalSettings
    {
        private PropertyTable props;
        private Document doc;

        public enum FrameRate
        {
            FrameRate_DEFAULT = 0,
            FrameRate_120 = 1,
            FrameRate_100 = 2,
            FrameRate_60 = 3,
            FrameRate_50 = 4,
            FrameRate_48 = 5,
            FrameRate_30 = 6,
            FrameRate_30_DROP = 7,
            FrameRate_NTSC_DROP_FRAME = 8,
            FrameRate_NTSC_FULL_FRAME = 9,
            FrameRate_PAL = 10,
            FrameRate_CINEMA = 11,
            FrameRate_1000 = 12,
            FrameRate_CINEMA_ND = 13,
            FrameRate_CUSTOM = 14,
        };

        public PropertyTable Props
        {
            get
            {
                return props;
            }
        }

        public Document Document
        {
            get
            {
                return doc;
            }
        }

        public readonly SimpleProperty<int> UpAxis;
        public readonly SimpleProperty<int> UnAxisSign;
        public readonly SimpleProperty<int> FrontAxis;
        public readonly SimpleProperty<int> FrontAxisSign;
        public readonly SimpleProperty<int> CoordAxis;
        public readonly SimpleProperty<int> CoordAxisSign;
        public readonly SimpleProperty<int> OriginalUpAxis;
        public readonly SimpleProperty<int> OriginalUpAxisSign;
        public readonly SimpleProperty<double> UnitScaleFactor;
        public readonly SimpleProperty<double> OriginalUnitScaleFactor;
        public readonly SimpleProperty<Vector3> AmbientColor;
        public readonly SimpleProperty<string> DefaultCamera;

        public readonly SimpleProperty<FrameRate> TimeMode;
        public readonly SimpleProperty<ulong> TimeSpanStart;
        public readonly SimpleProperty<ulong> TimeSpanStop;
        public readonly SimpleProperty<float> CustomFrameRate;

        public FileGlobalSettings(Document doc, PropertyTable prop)
        {
            this.doc = doc;
            this.props = prop;

            UpAxis = new SimpleProperty<int>(prop, "UpAxis", 1);
            UnAxisSign = new SimpleProperty<int>(prop, "UnAxisSign", 1);
            FrontAxis = new SimpleProperty<int>(prop, "FrontAxis", 2);
            FrontAxisSign = new SimpleProperty<int>(prop, "FrontAxisSign", 1);
            CoordAxis = new SimpleProperty<int>(prop, "CoordAxis", 0);
            CoordAxisSign = new SimpleProperty<int>(prop, "CoordAxisSign", 0);
            OriginalUpAxis = new SimpleProperty<int>(prop, "OriginalUpAxis", 0);
            OriginalUpAxisSign = new SimpleProperty<int>(prop, "OriginalUpAxisSign", 0);
            UnitScaleFactor = new SimpleProperty<double>(prop, "UnitScaleFactor", 0);
            OriginalUnitScaleFactor = new SimpleProperty<double>(prop, "OriginalUnitScaleFactor", 0);
            AmbientColor = new SimpleProperty<Vector3>(prop, "AmbientColor", new Vector3(0, 0, 0));
            DefaultCamera = new SimpleProperty<string>(prop, "DefaultCamera", "");

            TimeMode = new SimpleProperty<FrameRate>(prop, "TimeMode", FrameRate.FrameRate_DEFAULT);
            TimeSpanStart = new SimpleProperty<ulong>(prop, "TimeSpanStart", 0);
            TimeSpanStop = new SimpleProperty<ulong>(prop, "TimeSpanStop", 0);
            CustomFrameRate = new SimpleProperty<float>(prop, "CustomFrameRate", -1f);
        }
    }
}
