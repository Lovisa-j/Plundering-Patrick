﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Readable : Interactable
{
    public string readableName;

    [TextArea(3, 10)]
    public string textContent;

    public override void Interact(Transform interactingTransform)
    {
        if (interactingTransform.GetComponent<PlayerUI>())
            interactingTransform.GetComponent<PlayerUI>().ReadText(textContent);
    }
}
