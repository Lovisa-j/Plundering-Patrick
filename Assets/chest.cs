using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class chest : Interactable
{
    

    public override void Interact(Transform interactingTransform)
    {
        anim.SetBool("open", !anim.GetBool("open"));
        base.Interact(interactingTransform);        
    }

}
