using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TreeGrowingGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    void OnEnable()
    {
        Debug.Log($"Script Start");
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Editor causes this Update");
    }
}
