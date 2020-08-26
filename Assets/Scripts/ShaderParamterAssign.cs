using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderParamterAssign : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Material wall = null;

        foreach (Material m in GetComponent<MeshRenderer>().materials)
        {
            if (m.name.StartsWith("Wall"))
            {
                wall = m;
                break;
            }
        }

        if (wall != null)
        {
            Bounds b = GetComponent<MeshRenderer>().bounds;
            Vector3 minCoords = b.center - b.extents;
            Vector3 maxCoords = b.center + b.extents;

            wall.SetVector("_MinCoords", minCoords);
            wall.SetVector("_MaxCoords", maxCoords);
            //Debug.Log($"Set coords of material {wall}");
        }
    }
}
