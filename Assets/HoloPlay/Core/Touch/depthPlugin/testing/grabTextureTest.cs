using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Runtime.InteropServices;

namespace HoloPlay
{
    public class grabTextureTest : MonoBehaviour
    {

        public Material colorMat;
        public Material depthMat;
        public Material pMat;

        // public LKG.depthPluginStatic depth;

        void Start()
        {
            if (colorMat)
                colorMat.mainTexture = HPInput.CameraStreams.GetColor();
            if (depthMat)
                depthMat.mainTexture = HPInput.CameraStreams.GetDepth();
            if (pMat)
                pMat.mainTexture = HPInput.CameraStreams.GetProcessed();
        }

    }
}