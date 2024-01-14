using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnClick(bool buttonState)
    {
        //Get animator
        Animator animator = gameObject.GetComponent<Animator>();
        //Set the boolean value of the animator to the opposite of what it is
        animator.SetBool("Close", !animator.GetBool("Close"));

        //If the animator is set to close, set the text of the button to "Open Menu"
        if (animator.GetBool("Close"))
        {
            gameObject.GetComponentInChildren<UnityEngine.UI.Text>().text = "Open Menu";
        }
        //If the animator is set to open, set the text of the button to "Close Menu"
        else
        {
            gameObject.GetComponentInChildren<UnityEngine.UI.Text>().text = "Close Menu";
        }

    }
}
