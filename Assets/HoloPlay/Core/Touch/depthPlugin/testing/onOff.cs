using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloPlay
{
    public class onOff : MonoBehaviour
    {

        public GameObject toggle;
        public KeyCode key;

        // Update is called once per frame
        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(key))
                toggle.SetActive(!toggle.activeSelf);

        }
    }
}
