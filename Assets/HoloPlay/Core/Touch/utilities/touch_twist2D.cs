using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//effectively this is a 2D twist algorithm, applied to the given transform from the pov of the holoplay capture.
//if you want to use this together with the touch rotate component, apply this to the parent of the transform affected by the rotate.

namespace HoloPlay
{
    public class touch_twist2D : MonoBehaviour
    {
        public Transform twistingObject;
        public float touchRange = 1f;
        public float speedModifier = 1f;
        public bool reverse;

        const int minFramesWithTouches = 3;

        float startRot = 0f;
        float currentTwist = 0f;
        float startAngle;
        float lastFrameTwist;
        int lastFrameHadDoubleTouches = 0;

        private void Update()
        {
            bool useDoubleTouch = false;
            if (depthPluginStatic.touches.Count > 1)
            {
                //* not used, but re-enable if needed */
                // Vector3 newPos1 = depthPluginStatic.touches[0].GetLocalPos();
                Vector3 offset1 = depthPluginStatic.touches[0].GetWorldPos() - twistingObject.position;
                //* not used, but re-enable if needed */
                // Vector3 newPos2 = depthPluginStatic.touches[1].GetLocalPos();
                Vector3 offset2 = depthPluginStatic.touches[1].GetWorldPos() - twistingObject.position;
                if (offset1.sqrMagnitude < touchRange * touchRange && offset2.sqrMagnitude < touchRange * touchRange) //if within range...
                {
                    lastFrameHadDoubleTouches += 1;

                    if (lastFrameHadDoubleTouches >= minFramesWithTouches)
                    {
                        useDoubleTouch = true;
                        if (lastFrameHadDoubleTouches > minFramesWithTouches + minFramesWithTouches)
                            lastFrameHadDoubleTouches = minFramesWithTouches + minFramesWithTouches; //limit, with buffer. This allows a flaky frame to be ignored.
                    }
                    else // if (lastFrameHadDoubleTouches < minFramesWithTouches) //allow at least x frames of info before doing anything to prevent jumping once a touch is detected
                    {
                        if (lastFrameHadDoubleTouches == minFramesWithTouches - 1) //we are just about to grab it, so establish a 'grab point'
                        {
                            resetTwist();
                        }
                        return;// false;
                    }
                }
                    
            }
            
            if (useDoubleTouch)
            {
                //determine the rotation of the twist.
                float a = Utils.AngleBetweenPoints(depthPluginStatic.touches[0].GetLocalPos(), depthPluginStatic.touches[1].GetLocalPos());

                a -= startAngle;
                currentTwist = a;

                if (!reverse)
                    currentTwist = -currentTwist;

                currentTwist *= speedModifier;

                if (Mathf.Abs(currentTwist - lastFrameTwist) > 10f) //if its noisy, it could mean that the touches flipped, so reset.
                {
                    resetTwist();
                    return;// false;
                }
                else
                {
                    twistingObject.rotation = Capture.Instance.transform.rotation *  Quaternion.Euler(0f, 0f, currentTwist + startRot);
                    lastFrameTwist = currentTwist;
                }

                return;// true;
            }

            //nothing detected at all or out of range.
            lastFrameHadDoubleTouches -= 1;
                
            if (lastFrameHadDoubleTouches < 0)
                lastFrameHadDoubleTouches = 0;

            return;// false;
        }



        void resetTwist()
        {
            startRot += currentTwist;
            lastFrameTwist = currentTwist;
            currentTwist = 0f;
            startAngle = Utils.AngleBetweenPoints(depthPluginStatic.touches[0].GetLocalPos(), depthPluginStatic.touches[1].GetLocalPos());
        }
    }
}
