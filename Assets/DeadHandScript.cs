using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadHandScript : MonoBehaviour {

    public GameObject backgroundObj;
    SpriteRenderer backgroundImg;
    

    private void Awake()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log("iasbon ");
    }
}
