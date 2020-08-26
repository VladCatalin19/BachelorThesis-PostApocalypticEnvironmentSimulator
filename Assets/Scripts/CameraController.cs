using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float radius = Constants.BuildExplosionRadius;
    public float force = Constants.BuildExplosionForce;
    private Dictionary<RaycastHit, Fragmenter.Stats> runningFragmentations;
    private Dictionary<RaycastHit, Fragmenter.Stats> runningExplosions;
    private LayerMask onlyHits;

    void Start()
    {
        runningFragmentations = new Dictionary<RaycastHit, Fragmenter.Stats>();
        runningExplosions = new Dictionary<RaycastHit, Fragmenter.Stats>();
        onlyHits = LayerMask.GetMask(new string[] 
        {
            Constants.DestroyableStr,
            Constants.FrozenFragmentStr,
            Constants.MovingFragmentStr
        });
    }

    void Update()
    {
        // Check if user clicked on a mesh with active collider
        if (Input.GetMouseButtonDown(0) &&
            Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity) &&
            !runningFragmentations.ContainsKey(hit) && !runningExplosions.ContainsKey(hit))
        {

            if (!Fragmenter.IsFragmented(hit.transform.gameObject))
            {
                Fragmenter.Stats fs = new Fragmenter.Stats();
                runningFragmentations.Add(hit, fs);
                Debug.Log($"Fragmentation Start Time: {Time.timeSinceLevelLoad}");
                StartCoroutine(Fragmenter.Fragment(hit.transform.gameObject, fs));
            }
            else
            {
                Fragmenter.Stats fs = new Fragmenter.Stats();
                runningExplosions.Add(hit, fs);
                StartCoroutine(Fragmenter.Explode(hit.point, radius, force, fs));
            }
        }

        // When fragmentation finishes, create an explosion where user clicked
        List<RaycastHit> toRemove = new List<RaycastHit>();
        foreach (KeyValuePair<RaycastHit, Fragmenter.Stats> entry in runningFragmentations)
        {
            if (entry.Value.isDone)
            {
                Debug.Log($"Fragmentation Total Time: {entry.Value.totalTime / 1000.0f}");
                toRemove.Add(entry.Key);
            }
        }
        foreach (RaycastHit rhit in toRemove)
        {
            Fragmenter.Stats fs = new Fragmenter.Stats();
            runningFragmentations.Remove(rhit);
            runningExplosions.Add(rhit, fs);
            StartCoroutine(Fragmenter.Explode(rhit.point, radius, force, fs));
        }


        toRemove.Clear();
        foreach (KeyValuePair<RaycastHit, Fragmenter.Stats> entry in runningExplosions)
        {
            if (entry.Value.isDone)
            {
                toRemove.Add(entry.Key);
            }
        }
        foreach (RaycastHit rhit in toRemove)
        {
            runningExplosions.Remove(rhit);
        }
        /*
        for (int i = runningFragmentations.Count - 1; i >= 0; --i)
        {
            (Fragmenter.Stats stats, RaycastHit rhit) = runningFragmentations[i];

            if (stats.isDone)
            {
                StartCoroutine(Fragmenter.Explode(rhit.point, radius, force, null));
                runningFragmentations.RemoveAt(i);
            }
        }
        */
    }
}
