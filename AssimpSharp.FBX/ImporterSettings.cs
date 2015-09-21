using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp.FBX
{
    /// <summary>
    /// FBX importer runtime configuration
    /// </summary>
    public struct ImporterSettings
    {
        public static ImporterSettings Default
        {
            get
            {
                return new ImporterSettings()
                {

                    StrictMode = true,
                    ReadAllLayers = true,
                    ReadAllMaterials = false,
                    ReadMaterials = true,
                    ReadCameras = true,
                    ReadLights = true,
                    ReadAnimations = true,
                    ReadWeights = true,
                    PreservePivots = true,
                    OptimizeEmptyAnimationCurves = true,
                };
            }
        }

        public bool StrictMode;
        public bool ReadAllLayers;
        public bool ReadAllMaterials;
        public bool ReadMaterials;
        public bool ReadCameras;
        public bool ReadLights;
        public bool ReadAnimations;
        public bool ReadWeights;
        public bool PreservePivots;
        public bool OptimizeEmptyAnimationCurves;
    }
}
