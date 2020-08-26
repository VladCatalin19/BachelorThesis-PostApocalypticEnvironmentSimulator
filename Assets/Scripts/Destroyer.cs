using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class Destroyer : MonoBehaviour
{
    private class DestroyableGameObject
    {
        public enum StateType { fragmenting, exploding, decaying, destroyed, fragmented }

        public StateType state;
        public GameObject gameObject;
        public Fragmenter.Stats fragmenterStat;

        public DestroyableGameObject(GameObject gameObject)
        {
            this.gameObject = gameObject;
            state = StateType.decaying;
            fragmenterStat = null;
        }
    }

    private List<DestroyableGameObject> destroyableMisc;
    private List<DestroyableGameObject> destroyableBuild;
    private List<DestroyableGameObject> processingMisc;
    private List<DestroyableGameObject> processingBuild;

    public int seed = 0;

    // Time when windows and doors will start to destruct
    private float destructionMiscStart;
    // Time when walls and roofs will start to destruct
    private float destructionBuildStart;

    // Start is called before the first frame update
    void Start()
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("DestroyableParent");
        if (gameObjects == null || gameObjects.Length == 0)
        {

        }

        //Debug.Log($"Destroyer: Destroyable objects count: {gameObjects.Length}");

        destroyableMisc = new List<DestroyableGameObject>();
        destroyableBuild = new List<DestroyableGameObject>();
        processingMisc = new List<DestroyableGameObject>();
        processingBuild = new List<DestroyableGameObject>();

        foreach (GameObject go in gameObjects)
        {
            foreach (Transform child in go.transform)
            {
                bool wasAdded = false;

                if (child.name.StartsWith("Door"))
                {
                    //Debug.Log($"Destroyer: Child name with door: {child.name}");
                    destroyableMisc.Add(new DestroyableGameObject(child.gameObject));
                    wasAdded = true;
                }
                if (child.name.StartsWith("Window"))
                {
                    foreach (Transform window in child.transform)
                    {
                        //Debug.Log($"Destroyer: Child child name with window: {window.name}");
                        destroyableMisc.Add(new DestroyableGameObject(window.gameObject));
                    }
                    wasAdded = true;
                }
                if (child.name.StartsWith("Wall"))
                {
                    //Debug.Log($"Destroyer: Child child name with wall: {child.name}");
                    destroyableBuild.Add(new DestroyableGameObject(child.gameObject));
                    wasAdded = true;
                }

                if (!wasAdded)
                {
                    destroyableBuild.Add(new DestroyableGameObject(child.gameObject));
                }
            }
        }

        if (seed != 0)
        {
            Random.InitState(seed);
        }

        destructionMiscStart = Random.Range(Constants.DestroyerMiscMinStart, Constants.DestroyerMiscMaxStart);
        destructionBuildStart = Random.Range(Constants.DestroyerBuildMinStart, Constants.DestroyerBuildMaxStart);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeSinceLevelLoad > destructionMiscStart)
        {
            if (Random.Range(0.0f, 1.0f) < 0.05f && destroyableMisc.Count > 0)
            {
                ProcessDestroyable(destroyableMisc, processingMisc, true);
            }
        }
        DeprocessDestroyable(destroyableMisc, processingMisc, true);

        if (Time.timeSinceLevelLoad > destructionBuildStart)
        {
            if (Random.Range(0.0f, 1.0f) < 0.05f && destroyableBuild.Count > 0)
            {
                ProcessDestroyable(destroyableBuild, processingBuild, false);
            }
        }
        DeprocessDestroyable(destroyableBuild, processingBuild, false);
    }


    private void ProcessDestroyable(List<DestroyableGameObject> destroyable, List<DestroyableGameObject> processing, bool isMisc)
    {
        if (Random.Range(0.0f, 1.0f) < 0.25f && destroyable.Count > 0)
        {
            int index = Random.Range(0, destroyable.Count - 1);
            DestroyableGameObject toDestroy = destroyable[index];

            if (toDestroy.state == DestroyableGameObject.StateType.decaying)
            {
                Debug.Log($"Fragmenting {toDestroy.gameObject.name}");
                Fragmenter.Stats fs = new Fragmenter.Stats();
                toDestroy.fragmenterStat = fs;
                toDestroy.state = DestroyableGameObject.StateType.fragmenting;

                destroyable.RemoveAt(index);
                processing.Add(toDestroy);

                StartCoroutine(Fragmenter.Fragment(toDestroy.gameObject, fs, isMisc));
            }
            else if (toDestroy.state == DestroyableGameObject.StateType.fragmented)
            {
                Debug.Log($"Exploding {toDestroy.gameObject.name}");
                Fragmenter.Stats fs = new Fragmenter.Stats();
                toDestroy.fragmenterStat = fs;
                toDestroy.state = DestroyableGameObject.StateType.exploding;

                destroyable.RemoveAt(index);
                processing.Add(toDestroy);

                StartCoroutine(Fragmenter.Explode(
                    CalculateExplosionPoint(toDestroy.gameObject),
                    isMisc ? Constants.MiscExplosionRadius : Constants.BuildExplosionRadius,
                    isMisc ? Constants.MiscExplosionForce  : Constants.BuildExplosionForce,
                    fs)
                );
            }
            else
            {
                Debug.Log("Oops");
            }
        }
    }


    private void DeprocessDestroyable(List<DestroyableGameObject> destroyable, List<DestroyableGameObject> processing, bool isMisc)
    {
        for (int i = processing.Count - 1; i >= 0; --i)
        {
            DestroyableGameObject toProcess = processing[i];

            if (toProcess.fragmenterStat != null && toProcess.fragmenterStat.isDone)
            {
                if (toProcess.state == DestroyableGameObject.StateType.fragmenting)
                {
                    Fragmenter.Stats fs = new Fragmenter.Stats();
                    toProcess.gameObject = toProcess.fragmenterStat.fragmentedGameObject;
                    toProcess.fragmenterStat = fs;
                    toProcess.state = DestroyableGameObject.StateType.exploding;

                    Debug.Log($"Exploding {toProcess.gameObject.name}");

                    StartCoroutine(Fragmenter.Explode(
                        CalculateExplosionPoint(toProcess.gameObject, true),
                        isMisc ? Constants.MiscExplosionRadius : Constants.BuildExplosionRadius,
                        isMisc ? Constants.MiscExplosionForce  : Constants.BuildExplosionForce,
                        fs,
                        isMisc)
                    );
                }
                else if (toProcess.state == DestroyableGameObject.StateType.exploding)
                {
                    processing.RemoveAt(i);

                    if (toProcess.fragmenterStat.destroyedAll)
                    {
                        toProcess.state = DestroyableGameObject.StateType.destroyed;
                    }
                    else
                    {
                        toProcess.state = DestroyableGameObject.StateType.fragmented;
                        destroyable.Add(toProcess);
                    }
                }
            }
        }
    }


    private Vector3 CalculateExplosionPoint(GameObject gameObject, bool firstExplosion = false)
    {
        Bounds bounds = new Bounds(gameObject.transform.position, Vector3.zero);
        foreach (MeshRenderer renderer in gameObject.GetComponentsInChildren<MeshRenderer>())
        {
            bounds.Encapsulate(renderer.bounds);
        }

        float endY = firstExplosion ? -0.3f : 1.0f;

        Vector3 offset = new Vector3(
            Random.Range(-1.0f, 1.0f),
            Random.Range(-1.0f, endY),
            Random.Range(-1.0f, 1.0f)
        );

        Vector3 point = bounds.center + Vector3.Scale(bounds.extents, offset);

        //Debug.Log($"Destroyer: {gameObject.name} bounds {bounds.ToString("F5")}");
        //Debug.Log($"Destroyer: Explosion point: {point.ToString("F5")}");

        return point;
    }
}
