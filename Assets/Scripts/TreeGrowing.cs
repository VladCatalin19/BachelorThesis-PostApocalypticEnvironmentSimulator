using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGrowing : MonoBehaviour
{
    private float lastRefresh = 0.0f;
    private float refreshInterval = 10.0f;
    private int index = 0;
    private List<GameObject> children;
    public bool isGrowing = true;

    // Start is called before the first frame update
    void Start()
    {
        children = new List<GameObject>(transform.childCount);

        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
            child.GetComponent<MeshRenderer>().enabled = false;
        }

        index = 0;
        refreshInterval += Random.Range(0.0f, 10.0f);
        children[0].GetComponent<MeshRenderer>().enabled = true;
        //Debug.Log($"Children Cound: {children.Count}");
    }

    // Update is called once per frame
    void Update()
    {
        if (isGrowing && Time.time > lastRefresh + refreshInterval
            && index <= children.Count - 1
            && Random.Range(0.0f, 1.0f) < 0.5f)
        {
            lastRefresh = Time.time;

            children[index].GetComponent<MeshRenderer>().enabled = false;
            index = Mathf.Clamp(index + 1, 0, children.Count - 1);
            children[index].GetComponent<MeshRenderer>().enabled = true;
            //Debug.Log($"Set child {index} visible");
        }
    }
}
