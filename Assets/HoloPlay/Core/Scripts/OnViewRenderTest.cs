//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System.Collections;
using System.Collections.Generic;

using HoloPlay;

using UnityEngine;

namespace HoloPlaySDK_Tests
{
    public class OnViewRenderTest : MonoBehaviour
    {
        //Make sure to subscribe when enabled and unsubscribe to prevent memory leaks
        void OnEnable()
        {
            Capture.onViewRender += FlipCubeOnView;
        }

        void OnDisable()
        {
            Capture.onViewRender -= FlipCubeOnView;
        }

        void FlipCubeOnView(int viewIndex, int numViews)
        {
            int segment = Mathf.FloorToInt(viewIndex * 3f / numViews);
            switch (segment)
            {
                case 1:
                    GetComponent<MeshRenderer>().material.color = Color.white;
                    break;
                case 0:
                case 2:
                    GetComponent<MeshRenderer>().material.color = Color.red;
                    break;
                default:
                    break;
            }
        }
    }
}