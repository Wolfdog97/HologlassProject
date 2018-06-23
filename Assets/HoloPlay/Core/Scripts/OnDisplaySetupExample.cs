using UnityEngine;
using UnityEngine.UI;

namespace HoloPlay
{
    public class OnDisplaySetupExample : MonoBehaviour
    {
        public void OnDisplaySetup()
        {
            var text = GetComponent<Text>();
			
            if (ExtendedUICamera.secondScreen)
            {
                text.text += "<color=green> • UI Second Screen :)</color>";
            }
            else
            {
                text.text += "<color=yellow> • UI Single Screen</color>";
            }
        }
    }
}