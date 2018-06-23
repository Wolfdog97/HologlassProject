using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloPlay
{

    public class touch_move : MonoBehaviour
    {

        public Transform objectToMove;
        public float touchRange = 2f;
        [Tooltip("The script will let go when more or less than 2 touches are detected or when the 2 touches are farther apart than this local distance.")]
        public float autoLetGoRange = .3f;

        public bool snapCenterToTouch = false;

        public AudioClip grabSound;
        public AudioSource audioSource;

        Collider hasCollider;

        void Awake()
        {
            if (!objectToMove)
                objectToMove = transform; //use ourselves if nothing is provided

            hasCollider = objectToMove.GetComponent<Collider>();

            //if there is a clip, obviously the dev wants it to make a sound. so make sure it works.
            if (grabSound)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        int grabbedLastFrame = 0;
        Vector3 grabOffset = Vector3.zero;
        int missingGrabFrames = 0;  //used to keep the grab from bugging out if the hand completely disappears from the screen, it won't think we are still grabbing it.
        Vector3 ave;
        private void Update()
        {
            if (!HoloPlay.depthPluginStatic.Get())
                return;


            //this if is a shorthand to keep something 'logically' grabbed.
            //without it, the touches easily go outside of the bounding boxes. 
            //with it,  as long as 2 touches are near each other the object is considered still grabbed.
            if (grabbedLastFrame == 1) //we still have 2 fingers
            {
                Vector3 offset = Vector3.zero;
                if (depthPluginStatic.touches.Count == 2)
                        offset = depthPluginStatic.touches[0].GetLocalPos() - depthPluginStatic.touches[1].GetLocalPos(); //TODO switch this to cm or something more relevant.

                if (depthPluginStatic.touches.Count == 2 && offset.sqrMagnitude > autoLetGoRange * autoLetGoRange) //if two touches outside the range...                    
                {
                    //let go.  ... we'll find out normally, after this if else.
                    //note that we don't check if touches are IN grabbing range... only if they are definitely OUT. THEN, we let go.
                }
                else //consider things grabbed.
                {
                    if (depthPluginStatic.touches.Count == 0)
                        missingGrabFrames++;
                    else
                        missingGrabFrames = 0;

                    if (missingGrabFrames < 8) //how many frames with 0 touches allowed before we say that we should automatically let go.
                    {
                        if (depthPluginStatic.touches.Count > 1)
                            ave = depthPluginStatic.Get().averageWorld;
                        if (snapCenterToTouch)
                            objectToMove.position = ave;
                        else
                            objectToMove.position = ave + grabOffset;
                        return;
                    }
                }

            }

            if (depthPluginStatic.touches.Count > 1)
            {
                int total = 0;
                Vector3 average = Vector3.zero;
                foreach (AirTouch t in depthPluginStatic.touches)
                {
                    Vector3 offset = t.GetWorldPos() - objectToMove.position;
                    if (offset.sqrMagnitude < touchRange * touchRange) //if within range...
                    {
                        if (!hasCollider)
                        {
                            total++;
                            average += t.GetWorldPos();
                        }
                        else if (hasCollider.bounds.Contains(t.GetWorldPos())) //if it has a collider, use that to do the test.
                        {
                            total++;
                            average += t.GetWorldPos();
                        }
                    }
                }

                if (total > 1)
                {
                    average /= total;

                    if (grabbedLastFrame < -10) //a new grab.
                    { 
                        audioSource.PlayOneShot(grabSound);
                        grabOffset = objectToMove.position - average ;
                    }
                    grabbedLastFrame = 1;



                    if (snapCenterToTouch)
                        objectToMove.position = average;
                    else
                        objectToMove.position = average + grabOffset;
                }
                else
                    grabbedLastFrame--;

            }
            else
                grabbedLastFrame--;
        }
    }

}