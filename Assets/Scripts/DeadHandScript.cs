using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(BoxCollider))]
public class DeadHandScript : MonoBehaviour {

    public GameObject newBackgroundObj;
    public GameObject nextTrigger;
    //public GameObject deactivateObj;


    private void Start()
    {
       // StartCoroutine(WaitToChange());
    }

    public enum Trigger
    {
        Trigger1,
        Trigger2,
        Trigger3,
        Trigger4,
        Trigger5
    }

    public Trigger triggers;

    private void OnTriggerExit(Collider other)
    {
        switch (triggers)
        {
            case Trigger.Trigger1:
                Debug.Log("Trigger 1 is a go!!");
                StartCoroutine(WaitToChange());
                //SetNextTrigger();
                break;
            case Trigger.Trigger2:
                Debug.Log("Trigger 2 is a go!!");
                StartCoroutine(WaitToChange());
                //SetNextTrigger();
                break;
            case Trigger.Trigger3:
                Debug.Log("Trigger 3 is a go!!");
                StartCoroutine(WaitToChange());
                //SetNextTrigger();
                break;
            case Trigger.Trigger4:
                Debug.Log("Trigger 4 is a go!!");
                StartCoroutine(WaitToChange());
                //SetNextTrigger();
                break;
            case Trigger.Trigger5:
                Debug.Log("Trigger 5 is a go!!");
                StartCoroutine(WaitToChange());
                //SetNextTrigger();
                // play something transition scene??? or wait some time and change scene
                break;
            default:
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (triggers)
        {
            case Trigger.Trigger1:
                ChangeBackground();
                break;
            case Trigger.Trigger2:
                ChangeBackground();
                break;
            case Trigger.Trigger3:
                ChangeBackground();
                break;
            case Trigger.Trigger4:
                ChangeBackground();
                break;
            case Trigger.Trigger5:
                ChangeBackground();
                break;
            default:
                break;
        }
    }

    void ChangeBackground()
    {
        newBackgroundObj.SetActive(true);
        //add some ting?
    }

    void DisableBackground()
    {
        newBackgroundObj.SetActive(false);
        //Maybe do other stuff too???
    }

    void SetNextTrigger()
    {
        Destroy(this.gameObject);

        if (!nextTrigger)
        {
            Debug.LogWarning("Trigger is empty");
            DisableBackground();
        }
        else
        {
            nextTrigger.SetActive(true);
        }
    }

    IEnumerator WaitToChange()
    {
        yield return new WaitForSeconds(10);
        DisableBackground();
        SetNextTrigger();
        Debug.Log("running");
    }

}
