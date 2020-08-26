using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointBreakListener : MonoBehaviour
{
    private void OnJointBreak(float breakForce)
    {
        FragmentsGraph fg = GetComponentInParent<FragmentsGraph>();

        if (fg != null)
        {
            HingeJoint[] hjs = GetComponents<HingeJoint>();
            if (hjs != null)
            {
                foreach (HingeJoint hj in hjs)
                {
                    if (hj != null && hj.connectedBody != null && hj.connectedBody.gameObject != null)
                    {
                        fg.updateNextFrame.Enqueue(new FragmentsGraph.HingeJointPair(gameObject, hj.connectedBody.gameObject, hj));
                    }
                }
            }
        }
    }
}
