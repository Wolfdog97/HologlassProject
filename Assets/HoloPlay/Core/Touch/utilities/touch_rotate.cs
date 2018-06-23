//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.


//a single finger rotate script

using System.Collections;
using System.Collections.Generic;
using HoloPlay;
using UnityEngine;

public class touch_rotate : MonoBehaviour
{

    public Transform objectToRotate;
    [Tooltip("How close to an object must a touch be to cause it to rotate.")]
    public float touchRange = 1f;
    [Tooltip("If the touch is too close to the rotation pivot, the object can flip out. This allows an inner cutoff to prevent this.")]
    public float innerCutoff = .12f;
    [Tooltip("Change this if you don't want your hand movement to match rotation 1:1")]
    public float rotSpeedModifier = 1f;

    [Range(0f, 1f)]
    public float inertiaMod = .9f;
    
    public bool x = true;
    public bool y = true;

    public bool invertX = false;
    public bool invertY = false;

    //this helps handle noisy input by requiring that x consecutive frames have touches before using the input.
    //it also allows for x skipped frames before resetting the 'grab' point for rotation
    //this number should be > 0
    const int minFramesWithTouches = 3; 


    ///////////////// audio vars
    [SerializeField]
    AudioClip clickClip;
    AudioSource audioSource;

    //Time delay between audio to prevent unpleasant sound
    float clickDelayTimer;
    float clickDelayTime = 0.05f;

    //Angle change to trigger click
    [SerializeField]
    float rotClickAmount = 80f;
    //* not used, but re-enable if needed */
    // [SerializeField]
    // float maxSoundRot = 100;



    // Use this for initialization
    void Awake()
    {
        if (!objectToRotate)
            objectToRotate = transform; //use ourselves if nothing is provided
        
        //if there is a clip, obviously the dev wants it to make a sound. so make sure it works.
        if (clickClip)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

    }


    Vector3 lastPosition; 
    int lastFrameHadTouches = 0; //prevents snapping on first touch detection

    Quaternion startRot; //the rotation of the object at the time it was 'grabbed'
    Quaternion grabRot; //the rotation/orientation of the grab point.
    Vector3 grabPos;

    //inertia
    Quaternion lastFrameMove;
    Quaternion inertiaStartRot;
    Quaternion lastFrameRot;
    float currentInertia; //starts at 1 once the object has been rotated, turns down to 0 slerp over inertiaMod time.
    //* not used, but re-enable if needed */
    // float currentLerp;
    //* not used, but re-enable if needed */
    // Quaternion lastDiff;
    //* not used, but re-enable if needed */
    // Quaternion currentDiff;

    private void Update()
    {
        if (!HoloPlay.depthPluginStatic.Get())
            return;

        bool haveTouches = false;
        Vector3 newPos = lastPosition;
        if (HoloPlay.depthPluginStatic.touches.Count == 1)
        {
            newPos = getCurrentTouchPos(true);
            //newPos = HoloPlaySDK.depthPlugin.touches[0].getLocalPos();
            if (invertX)
                newPos.x = -newPos.x;
            if (invertY)
                newPos.y = -newPos.y;

            if (!x)
                newPos.x = 0f;
            if (!y)
                newPos.y = 0f;


            Vector3 offset = getCurrentTouchPos(true) - objectToRotate.position;
            //Vector3 offset = HoloPlaySDK.depthPlugin.touches[0].getWorldPos() - objectToRotate.position;
            if (offset.sqrMagnitude < touchRange * touchRange && offset.sqrMagnitude > innerCutoff * innerCutoff) //if within range...
            {
                haveTouches = true;
                lastFrameHadTouches += 1;
                if (lastFrameHadTouches > minFramesWithTouches + minFramesWithTouches)
                    lastFrameHadTouches = minFramesWithTouches + minFramesWithTouches; //limit, with buffer. This allows a flaky frame to be ignored.
                else if (lastFrameHadTouches < minFramesWithTouches) //allow at least x frames of info before doing anything to prevent jumping once a touch is detected
                {
                    if (lastFrameHadTouches == minFramesWithTouches - 1) //we are just about to grab it, so establish a 'grab point'
                    {
                        checkUpVector();
                        grabPos = newPos;
                        resetGrabTo();
                    }

                    lastPosition = newPos;  //set it back... touch wasn't within range
                    return;
                }
            }
        }   
        

        if (!haveTouches) 
        {
            
            lastFrameHadTouches -= 1;
            if (lastFrameHadTouches < 0)
                lastFrameHadTouches = 0;

            //no touches means lets try to use some INERTIA.
            if (inertiaMod > 0f && currentInertia > 0f)
            {
                //currentLerp += currentInertia;
                //objectToRotate.rotation = Quaternion.SlerpUnclamped(lastDiff, currentDiff, currentLerp) * startRot;
                //currentInertia *= inertiaMod - Time.deltaTime;

          ///      Quaternion diff = currentDiff * Quaternion.Inverse(lastDiff);
                objectToRotate.rotation =  objectToRotate.rotation * lastRotation;

               //  objectToRotate.rotation = Quaternion.Slerp(objectToRotate.rotation,  objectToRotate.rotation * lastRotation, currentInertia);
            }
        }
        else if (lastFrameHadTouches >= minFramesWithTouches) //we're moving things.
        {        
            checkUpVector();
            Quaternion angle = getRotationFromTo(objectToRotate.position, newPos);         
            Quaternion diff =  angle * Quaternion.Inverse(grabRot)  ; //the difference between where we are, and where the 'grab' started.

            //* not used, but re-enable if needed */
            // lastDiff = currentDiff;
            //* not used, but re-enable if needed */
            // currentDiff = diff;


            //if (rotSpeedModifier < 1f)
            //{
            //    diff = Quaternion.Slerp(Quaternion.identity, diff, rotSpeedModifier); 
            //}
            //else if (rotSpeedModifier > 1f)
            //{
            //    int times = (int)Mathf.Floor(rotSpeedModifier);
            //    for (int i = 1; i < times; i ++)
            //    {
            //        diff *= diff;
            //    }
            //    float fraction = rotSpeedModifier % 1;
            //    diff *= Quaternion.Slerp(Quaternion.identity, diff, fraction);
            //}



             Quaternion lastRot = objectToRotate.rotation;

            objectToRotate.rotation = diff * startRot ; //THE MEAT
            lastRotation = Quaternion.Inverse(objectToRotate.rotation) * lastRot;

            currentInertia = 1f;
            //* not used, but re-enable if needed */
            // currentLerp = 1f;

            if (lastPosition != newPos)
            {
                
                //      MatchSoundVolumeToMovement();
                MakeClickNoise(objectToRotate);
            }

        }

        //TODO enable inertia

        //  inertia = Quaternion.Inverse(objectToRotate.rotation) * lastRotation; //inertia is the difference in rotation.

        lastPosition = newPos;
        //lastFrameRot = objectToRotate.rotation;
    }


    enum upVectorEnum
    {
        UP,
        RIGHT,
        FORWARD
    }
    upVectorEnum upVector;

    Vector3 getCurrentTouchPos(bool world = false)
    {
        if (!world)
            return HoloPlay.depthPluginStatic.Get().averageNormalized;
        else
            return HoloPlay.depthPluginStatic.Get().averageWorld;
    }

    void resetGrabTo ()
    {
        startRot = objectToRotate.rotation;
        grabRot =  getRotationFromTo(objectToRotate.position, grabPos); 
    }

    Quaternion getRotationFromTo(Vector3 from, Vector3 to)
    {
        Vector3 relativePos = to - from;
        if (upVector == upVectorEnum.UP)
            return Quaternion.LookRotation(relativePos, objectToRotate.up);
        else if (upVector == upVectorEnum.RIGHT)
            return Quaternion.LookRotation(relativePos, objectToRotate.right);
        else 
            return Quaternion.LookRotation(relativePos, objectToRotate.forward);
    }

    //we have to make sure that the up vector does not align with the holoplayer view or we will get rotation artifacts.
    void checkUpVector()
    {

        Vector3 currentTouchVector = getCurrentTouchPos(true) - objectToRotate.position;
        float current;
        if (upVector == upVectorEnum.UP)
            current = Mathf.Abs(Vector3.Dot(objectToRotate.up, currentTouchVector));
        else if (upVector == upVectorEnum.RIGHT)
            current = Mathf.Abs(Vector3.Dot(objectToRotate.right, currentTouchVector));
        else
            current = Mathf.Abs(Vector3.Dot(objectToRotate.forward, currentTouchVector));

        if (Mathf.Abs(current) > .5f) //keep it low for best behavior.  near 1 or -1 means the up vector's pole is facing our view.
        {
            //choose the best (lowest dot) up vector.
            //if the object's right is facing close to the camera, choose it's up.
            // Dot returns 1 if they point in exactly the same direction, -1 if they point in completely opposite directions and zero if the vectors are perpendicular
            float up = Mathf.Abs(Vector3.Dot(objectToRotate.up, currentTouchVector));
            float forward = Mathf.Abs(Vector3.Dot(objectToRotate.forward, currentTouchVector));
            float right = Mathf.Abs(Vector3.Dot(objectToRotate.right, currentTouchVector));

            upVectorEnum best;
            if (up < forward)
            {
                if (up < right)
                    best = upVectorEnum.UP;
                else
                    best = upVectorEnum.RIGHT;
            }
            else if (forward < right)
            {
                best = upVectorEnum.FORWARD;
            }
            else
            {
                best = upVectorEnum.RIGHT;
            }

            if (best != upVector)
            {
                upVector = best;
                grabPos = getCurrentTouchPos();
                resetGrabTo();
          //      Debug.Log("switched to " + best);
            }
        }
    }



    //void MatchSoundVolumeToMovement()
    //{
    //    float vol = Mathf.Clamp01(rot.magnitude / maxSoundRot);

    //    audioSource.volume = vol;
    //}

    Quaternion lastRotation;
    float totalRotation = 0; //Store the total rotation of the object to check if there should be a click

    void MakeClickNoise(Transform objectToRotate)
    {
        if (clickClip == null || audioSource == null)
        {
            return;
        }

        float change = Quaternion.Angle(objectToRotate.rotation, lastRotation);
        totalRotation += change;

        clickDelayTimer -= Time.deltaTime;

        if (totalRotation > rotClickAmount && clickDelayTimer < 0)
        {
            clickDelayTimer = clickDelayTime;
            totalRotation = 0;
            audioSource.PlayOneShot(clickClip);
        }

        lastRotation = objectToRotate.rotation;
    }

}