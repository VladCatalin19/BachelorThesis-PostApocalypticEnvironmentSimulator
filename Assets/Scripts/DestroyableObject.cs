using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct DestroyableObject
{
    public enum StateType { fragmenting, exploding, decaying, destroyed }

    public StateType state { get; private set; }
    public GameObject gameObject { get; private set; }

    public DestroyableObject(GameObject gameObject)
    {
        this.gameObject = gameObject;
        state = StateType.decaying;
    }
}
