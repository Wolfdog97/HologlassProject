using UnityEngine;
using System.Collections;

//a simple utility that makes objects show/hide when a button is pressed

namespace HoloPlay
{
    public class HideShow : MonoBehaviour
    {

        public KeyCode key = KeyCode.Space;

        [Tooltip("These will be shown/hidden when the key is pressed")]
        public GameObject[] toggleObjects;


        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(key))
                foreach (GameObject t in toggleObjects)
                {
                    t.SetActive(!t.activeSelf);
                }
        }



    }
}

