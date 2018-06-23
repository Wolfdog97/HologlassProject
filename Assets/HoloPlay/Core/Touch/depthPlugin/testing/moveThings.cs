using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloPlay
{
    public class moveThings : MonoBehaviour
    {

        public depthPluginBase b;

        public Transform[] things;

        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update()
        {
            int i = 0;
            foreach(AirTouch t in HPInput.AirTouches.Touches)
            {
                things[i].position = t.GetWorldPos();
                i++;
            }
    

    }
    }
}
