using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentsGraph : MonoBehaviour
{
    public struct HingeJointPair
    {
        public GameObject go1;
        public GameObject go2;
        public HingeJoint hj;

        public HingeJointPair(GameObject go1, GameObject go2, HingeJoint hj)
        {
            this.go1 = go1;
            this.go2 = go2;
            this.hj = hj;
        }
    }

    public Graph<GameObject, HingeJoint> graph = null;
    public HashSet<GameObject> anchoredFragments = null;
    public int initialAnchoredCount = 0;
    public int initialFragmentsCount = 0;
    public int currentFragmentsCount = 0;
    public Queue<HingeJointPair> updateNextFrame;
    private Queue<HingeJointPair> updateCurrentFrame;

    public bool showRays = true;

    private void Start()
    {
        updateNextFrame = new Queue<HingeJointPair>();
        updateCurrentFrame = new Queue<HingeJointPair>();
    }

    private void Update()
    {
        if (graph == null) return;

        if (updateCurrentFrame.Count > 0)
        {
            foreach (HingeJointPair pair in updateCurrentFrame)
            {
                if (pair.hj == null)
                {
                    //Debug.Log($"Found destroyed Hinge Joint {pair.go1.name} -> {pair.go2.name}");
                    if (graph.ContainsEdge(pair.go1, pair.go2))
                    {
                        graph.RemoveEdge(pair.go1, pair.go2);
                    }
                    if (graph.ContainsEdge(pair.go2, pair.go1))
                    {
                        graph.RemoveEdge(pair.go2, pair.go1);
                    }
                }
            }
            updateCurrentFrame.Clear();
        }

        if (updateNextFrame.Count > 0)
        {
            foreach (HingeJointPair pair in updateNextFrame)
            {
                updateCurrentFrame.Enqueue(pair);
            }
            updateNextFrame.Clear();
        }

        if (!Debug.isDebugBuild || !showRays) return;

        foreach (GameObject node in graph.GetNodes())
        {
            foreach (KeyValuePair<GameObject, HingeJoint> pair in graph.GetNeighbours(node))
            {
                Color rayColor = (anchoredFragments != null && anchoredFragments.Contains(pair.Key)) ? Color.green : Color.red;
                Debug.DrawLine(node.GetComponent<Renderer>().bounds.center,
                    pair.Key.GetComponent<Renderer>().bounds.center, rayColor);
            }
        }
    }
}
