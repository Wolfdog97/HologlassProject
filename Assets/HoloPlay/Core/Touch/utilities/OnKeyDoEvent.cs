using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnKeyDoEvent : MonoBehaviour
{

    public KeyCode key;
    public UnityEvent method;


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(key))
            method.Invoke();
    }
}