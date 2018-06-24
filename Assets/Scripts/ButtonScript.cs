using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonScript : MonoBehaviour {

    Animator buttonAnim;
    public bool animEnd;

    private void Update()
    {
        if (animEnd)
        {
            //SceneManager.LoadScene(1);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Play button animation
        buttonAnim = GetComponent<Animator>();
        if(buttonAnim != null)
        {
            Debug.Log("Playing anim");
            //buttonAnim.Play("");
        }

        SceneManager.LoadScene(1);

        // Play sound
        // Play TV animation
        // Camera movment? and bool change
        // Transition scene after a certain amount of time
    }
}
