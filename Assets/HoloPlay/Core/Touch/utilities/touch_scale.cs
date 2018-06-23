using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloPlay
{
    public class touch_scale : depthTouchTarget
    {

        Bounds touchBounds;

        // Use this for initialization
        void Start()
        {

        }

        public override void onDepthTouch(List<AirTouch> touches)
        {
           touchBounds = new Bounds();

            for (int i = 0; i < touches.Count; i++)
            {
                  touchBounds.Encapsulate(touches[i].GetLocalPos());
            }
        }
    }
}
