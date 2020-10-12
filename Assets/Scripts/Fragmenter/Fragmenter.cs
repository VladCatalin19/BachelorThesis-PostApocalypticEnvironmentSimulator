using GK;   // Voronoi functions
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using GameObject = UnityEngine.GameObject;
using Math = System.Math;
using Stopwatch = System.Diagnostics.Stopwatch;

public static class Fragmenter
{
    public class Stats
    {
        public bool isDone = false;
        public int fragments = 0;
        public GameObject fragmentedGameObject = null;
        public bool destroyedAll = false;
        public long totalTime = 0L;
    }


    private struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }


    /// <summary>
    /// Checks if a game object has been fragmented.
    /// </summary>
    /// <param name="gameObject">Game object to check.</param>
    /// <returns>true if game object is fragmented, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFragmented(GameObject gameObject)
    {
        return gameObject.CompareTag(Constants.FrozenFragmentStr)
            || gameObject.CompareTag(Constants.MovingFragmentStr);
    }


    public static IEnumerator Fragment(GameObject toFragmentObject, Stats returnedStats, bool areaInsteadOfSites = false)
    {
        returnedStats.totalTime = 0L;
        Debug.Log("Fragmentation started!");
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        // Gather fragmentation properties for the current game object. The program checks the
        // current game object and the topmost game object of the hierarchy for such properties.
        // If none are found, assume default properties.
        FragmentsProperties fragmentsProperties = toFragmentObject.GetComponent<FragmentsProperties>();
        if (fragmentsProperties == null)
        {
            GameObject root = toFragmentObject.transform.root.gameObject;
            FragmentsProperties rootFragmentsProperties = root.GetComponent<FragmentsProperties>();

            fragmentsProperties = (rootFragmentsProperties == null) 
                ? toFragmentObject.AddComponent<FragmentsProperties>() 
                : rootFragmentsProperties;
        }

        // Create new game object as parent of future generated fragments. This game object will
        // replace the current game object in the hierarchy and will retain its fragmentation properties.
        GameObject parentObject = new GameObject(toFragmentObject.name + Constants.FragmentStr);
        CopyTransforms(parentObject.transform, toFragmentObject.transform);
        parentObject.transform.SetParent(toFragmentObject.transform.parent, false);

        FragmentsProperties.Copy(parentObject.AddComponent<FragmentsProperties>(), fragmentsProperties);

        // Iterate through the mesh's triangles and divide each triangle in separate Voronoi diagrams.
        // The generated fragments will retain the original mesh's materials, normals and uv coordinates.
        int totalFragments = 0;
        Mesh mesh = toFragmentObject.GetComponent<MeshFilter>().mesh;

        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
        {
            int[] triangles = mesh.GetTriangles(subMeshIndex);
            // TODO paralelize
            for (int ti = 0; ti < triangles.Length; ti += 3)
            {
                Vertex[] v = new Vertex[3];
                for (int i = 0; i < 3; ++i)
                {
                    v[i].position = mesh.vertices[triangles[ti + i]];
                    // Assume zero values if missing normal or uv coordinates
                    v[i].normal = (mesh.normals.Length <= triangles[ti + i]) 
                        ? Vector3.zero 
                        : mesh.normals[triangles[ti + i]];
                    v[i].uv = (mesh.uv.Length <= triangles[ti + i]) 
                        ? Vector2.zero 
                        : mesh.uv[triangles[ti + i]];
                }
                List<GameObject> fragments = GenerateVoronoiFragments(v[0], v[1], v[2], fragmentsProperties, areaInsteadOfSites);

                foreach (GameObject fragment in fragments)
                {
                    if (fragment.GetComponent<MeshFilter>() != null)
                    {
                        // Create new game object with fragment's mesh
                        fragment.name = Constants.FrozenFragmentStr + totalFragments;
                        fragment.transform.SetParent(parentObject.transform, false);
                        fragment.tag = Constants.FrozenFragmentStr;

                        MeshRenderer goMeshRenderer = toFragmentObject.GetComponent<MeshRenderer>();
                        MeshRenderer fragmentMeshRenderer = fragment.AddComponent<MeshRenderer>();
                        int materialIdx = subMeshIndex % goMeshRenderer.materials.Length;
                        fragmentMeshRenderer.material = goMeshRenderer.materials[materialIdx];
                        // Hide current fragment's mesh until the destruction of the initial game object
                        // to prevent z-fighting effects.
                        fragmentMeshRenderer.enabled = false;

                        MeshCollider mc = fragment.AddComponent<MeshCollider>();
                        mc.convex = true;
                        Rigidbody rb = fragment.AddComponent<Rigidbody>();
                        rb.detectCollisions = false;
                        rb.isKinematic = true;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        rb.mass = fragmentsProperties.density * FragmentVolume(fragment);
                        rb.angularDrag = Constants.FragmentRBodyAngDrag;
                        rb.drag = Constants.FragmentRBodyDrag;

                        fragment.AddComponent<JointBreakListener>();

                        ++totalFragments;
                    }
                    else
                    {
                        // Missing fragment
                        Debug.Log($"Clipped site: {fragment.transform.localPosition}");
                    }
                }

                // Ensure the current function operation does not take a lot of time
                // in a single frame
                if (stopWatch.ElapsedMilliseconds > Constants.MaxTimeMs)
                {
                    Debug.Log("Fragmenter Yield in voronoi generation");
                    returnedStats.totalTime += stopWatch.ElapsedMilliseconds;
                    yield return null;
                    stopWatch.Restart();
                }
            }

            // Ensure the current function operation does not take a lot of time
            // in a single frame
            if (stopWatch.ElapsedMilliseconds > Constants.MaxTimeMs)
            {
                Debug.Log("Fragmenter Yield submesh parsing");
                returnedStats.totalTime += stopWatch.ElapsedMilliseconds;
                yield return null;
                stopWatch.Restart();
            }
        }

        Debug.Log($"Fragmentation Ended at: {returnedStats.totalTime / 1000.0f}");

        // Create an undirected graph with all fragments. Each fragment represents a node in the
        // graph. The edges represent the connectivity between the fragments. Each node will have
        // a direct edge to each of its touching neighbours.
        var graph = new Graph<GameObject, HingeJoint>();
        foreach (Transform child in parentObject.transform)
        {
            if (!child.CompareTag(Constants.FrozenFragmentStr))
            {
                continue;
            }

            GameObject go = child.gameObject;
            List<GameObject> possibleNeighbours = new List<GameObject>();

            // To find the touching neighbours, do a rough and fast bounding box intersection first.
            foreach (Transform child2 in parentObject.transform)
            {
                GameObject go2 = child2.gameObject;
                if (go != go2)
                {
                    Bounds b1 = go.GetComponent<MeshCollider>().bounds;
                    Bounds b2 = go2.GetComponent<MeshCollider>().bounds;

                    if (b1 == null) Debug.Log($"{go} does not have a Mesh Collider");
                    if (b2 == null) Debug.Log($"{go2} does not have a Mesh Collider");

                    if (b1 != null && b2 != null && b1.Intersects(b2))
                    {
                        possibleNeighbours.Add(go2);
                    }
                }
            }

            // Do a more fine and more computational heavy intersection. To do so, the mesh collider's
            // size will be slightly increased. The actual mesh collider's size cannot be modified,
            // thus a new game object will be created with a bigger mesh and a mesh collider.
            GameObject biggerGo = new GameObject($"Big_{child.name}");
            biggerGo.transform.SetParent(go.transform);
            CopyTransforms(biggerGo.transform, go.transform);
            
            biggerGo.AddComponent<MeshFilter>().mesh = ScaleMesh(go.GetComponent<MeshFilter>().mesh, Constants.MeshUpscaling);
            biggerGo.AddComponent<MeshCollider>().convex = true;

            if (!graph.ContainsNode(go))
            {
                graph.AddNode(go);
            }
            foreach (GameObject neighbour in possibleNeighbours)
            {
                Collider colBigg = biggerGo.GetComponent<Collider>();
                Collider colNeigh = neighbour.GetComponent<Collider>();

                if (Physics.ComputePenetration(
                    colBigg, go.transform.position, go.transform.rotation,
                    colNeigh, neighbour.transform.position, neighbour.transform.rotation, out _, out _))
                {
                    if (graph.ContainsEdge(go, neighbour) || graph.ContainsEdge(neighbour, go))
                    {
                        continue;
                    }

                    if (!graph.ContainsNode(neighbour))
                    {
                        graph.AddNode(neighbour);
                    }

                    void CreateJoint(GameObject go1, GameObject go2)
                    {
                        Vector3 center1 = FragmentCenter(go1);
                        Vector3 center2 = FragmentCenter(go2);

                        HingeJoint hj = go1.AddComponent<HingeJoint>();
                        hj.connectedBody = go2.GetComponent<Rigidbody>();
                        hj.enableCollision = false;
                        hj.breakForce = Constants.FragmentHingeBreakForce;
                        hj.anchor = center2;
                        hj.axis = center2 - center1;

                        JointLimits hinjeLimits = hj.limits;
                        hinjeLimits.min = Constants.FragmentHingeMinAngleDeg;
                        hinjeLimits.bounciness = 0.0f;
                        hinjeLimits.bounceMinVelocity = 0.0f;
                        hinjeLimits.max = Constants.FragmentHingeMaxAngleDeg;
                        hj.limits = hinjeLimits;
                        hj.useLimits = true;

                        graph.AddEdge(go1, go2, hj);
                    }

                    CreateJoint(go, neighbour);
                    CreateJoint(neighbour, go);
                }
            }
            //empty.transform.SetParent(null);
            Object.Destroy(biggerGo);

            // Ensure the current function operation does not take a lot of time
            // in a single frame.
            if (stopWatch.ElapsedMilliseconds > Constants.MaxTimeMs)
            {
                Debug.Log("Fragmenter Yield in graph creating");
                returnedStats.totalTime += stopWatch.ElapsedMilliseconds;
                yield return null;
                stopWatch.Restart();
            }
        }
        //Debug.Log($"Min Area: {minSurface}, Max Area: {maxSurface}");
        FragmentsGraph fragmentsGraph = parentObject.AddComponent<FragmentsGraph>();
        fragmentsGraph.graph = graph;

        // Chose anchor fragments. An anchor fragment is a fragment which is considered crucial in
        // a mesh structural stability. These have to be manually specified. The program decides
        // if a fragment is an anchor if it intersects a 3D object with "Anchor" tag and layer.
        AnchorList anchorList = toFragmentObject.GetComponent<AnchorList>();
        if (anchorList == null)
        {
            anchorList = toFragmentObject.transform.parent.gameObject.GetComponent<AnchorList>();
        }
        if (anchorList != null)
        {
            // Fast and rough intersection
            List<GameObject> possibleAnchored = new List<GameObject>();
            foreach (Transform fragment in parentObject.transform)
            {
                foreach (GameObject anchor in anchorList.gameObjects)
                {
                    Bounds b1 = fragment.GetComponent<Collider>().bounds;
                    Bounds b2 = anchor.GetComponent<BoxCollider>().bounds;
                    if (b1.Intersects(b2))
                    {
                        possibleAnchored.Add(fragment.gameObject);
                    }
                }
            }

            // Slow and accurate intersection
            HashSet<GameObject> anchoredFragments = new HashSet<GameObject>();
            foreach (GameObject fragment in possibleAnchored)
            {
                Collider col1 = fragment.GetComponent<Collider>();
                foreach (GameObject anchor in anchorList.gameObjects)
                {
                    Collider col2 = anchor.GetComponent<Collider>();
                    if (Physics.ComputePenetration(
                        col1, fragment.transform.position, fragment.transform.rotation,
                        col2, anchor.transform.position, anchor.transform.rotation, out _, out _))
                    {
                        anchoredFragments.Add(fragment);
                        //Debug.Log($"Anchored {fragment.name}");
                    }
                }
            }
            fragmentsGraph.anchoredFragments = anchoredFragments;
            fragmentsGraph.initialAnchoredCount = anchoredFragments.Count;
        }

        foreach (Transform child in parentObject.transform)
        {
            child.gameObject.GetComponent<MeshRenderer>().enabled = true;
            Rigidbody rb = child.gameObject.GetComponent<Rigidbody>();
            rb.isKinematic = fragmentsGraph.anchoredFragments != null && fragmentsGraph.anchoredFragments.Contains(rb.gameObject);
            rb.detectCollisions = true;
        }

        returnedStats.isDone = true;
        returnedStats.fragments = totalFragments;
        returnedStats.fragmentedGameObject = parentObject;
        fragmentsGraph.initialFragmentsCount = fragmentsGraph.currentFragmentsCount = totalFragments;

        Object.Destroy(toFragmentObject);
    }


    public static IEnumerator Explode(Vector3 hitPoint, float radius, float force, Stats returnedStats, bool immidiateHingeBreak = false)
    {
        Debug.Log("Explosion started!");
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        int fragmentsExploded = 0;

        Collider[] colliders = Physics.OverlapSphere(hitPoint, radius);

        HashSet<FragmentsGraph> graphsToFlood = new HashSet<FragmentsGraph>();
        HashSet<FragmentsGraph> graphsVisited = new HashSet<FragmentsGraph>();

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag(Constants.FrozenFragmentStr))
            {
                FragmentsGraph fragmentsGraph = collider.transform.parent.gameObject.GetComponent<FragmentsGraph>();
                if (fragmentsGraph != null)
                {
                    GameObject[] neighbours = new GameObject[fragmentsGraph.graph.GetNeighbours(collider.gameObject).Count];
                    fragmentsGraph.graph.GetNeighbours(collider.gameObject).Keys.CopyTo(neighbours, 0);

                    // Remove hinges
                    foreach (GameObject neighbour in neighbours)
                    {
                        Object.Destroy(fragmentsGraph.graph.GetEdgeData(collider.gameObject, neighbour));
                        Object.Destroy(fragmentsGraph.graph.GetEdgeData(neighbour, collider.gameObject));

                        fragmentsGraph.graph.RemoveEdge(collider.gameObject, neighbour);
                        fragmentsGraph.graph.RemoveEdge(neighbour, collider.gameObject);
                    }

                    fragmentsGraph.graph.RemoveNode(collider.gameObject);
                    if (fragmentsGraph.anchoredFragments != null)
                    {
                        fragmentsGraph.anchoredFragments.Remove(collider.gameObject);
                    }
                    fragmentsGraph.currentFragmentsCount--;

                    graphsVisited.Add(fragmentsGraph);
                }

                DestroyFragment(collider.gameObject, hitPoint, radius, force);
                ++fragmentsExploded;
            }
            // Apply force to already destroyed fragment.
            else if (collider.CompareTag(Constants.MovingFragmentStr))
            {
                Rigidbody rb = collider.gameObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(force, hitPoint, radius);
                }
            }

            // Ensure the current function operation does not take a lot of time
            // in a single frame.
            if (stopWatch.ElapsedMilliseconds > Constants.MaxTimeMs)
            {
                Debug.Log("Explosion Yield in parsing colliders");
                returnedStats.totalTime += stopWatch.ElapsedMilliseconds;
                yield return null;
                stopWatch.Restart();
            }
        }

        bool destroyedAll = false;

        void DestroyAllFragments(FragmentsGraph fg)
        {
            foreach (GameObject fragment in fg.graph.GetNodes())
            {
                foreach (KeyValuePair<GameObject, HingeJoint> pair in fg.graph.GetNeighbours(fragment))
                {
                    Object.Destroy(pair.Value, Random.Range(Constants.FragmentHingeMinDestroyDelay, Constants.FragmentHingeMaxDestroyDelay));
                }

                DestroyFragment(fragment, hitPoint, radius, force, immidiateHingeBreak);
            }
            fg.graph = null;
            fg.anchoredFragments = null;
            fg.initialAnchoredCount = 0;
            destroyedAll = true;
            Debug.Log("Destroyed all fragments");
        }

        foreach (FragmentsGraph fragmentsGraph in graphsVisited)
        {

            if (fragmentsGraph.currentFragmentsCount <= 0.25f * fragmentsGraph.initialFragmentsCount
                || fragmentsGraph.anchoredFragments == null)
            {
                DestroyAllFragments(fragmentsGraph);
            }
            else
            {
                // Flood fill
                Queue<GameObject> toVisit = new Queue<GameObject>();
                HashSet<GameObject> visited = new HashSet<GameObject>();

                foreach (GameObject anchoredFragmnt in fragmentsGraph.anchoredFragments)
                {
                    toVisit.Enqueue(anchoredFragmnt);
                }

                while (toVisit.Count > 0)
                {
                    GameObject fragment = toVisit.Dequeue();
                    visited.Add(fragment);

                    if (fragmentsGraph.graph.ContainsNode(fragment))
                    {
                        foreach (GameObject neighbour in fragmentsGraph.graph.GetNeighbours(fragment).Keys)
                        {
                            if (!visited.Contains(neighbour))
                            {
                                toVisit.Enqueue(neighbour);
                            }
                        }
                    }

                    // Ensure the current function operation does not take a lot of time
                    // in a single frame.
                    if (stopWatch.ElapsedMilliseconds > Constants.MaxTimeMs)
                    {
                        Debug.Log("Explosion Yield in flood fill");
                        returnedStats.totalTime += stopWatch.ElapsedMilliseconds;
                        yield return null;
                        stopWatch.Restart();
                    }
                }

                if (visited.Count <= 0.25f * fragmentsGraph.initialFragmentsCount)
                {
                    DestroyAllFragments(fragmentsGraph);
                }
            }
        }

        foreach (FragmentsGraph fragmentsGraph in graphsToFlood)
        {
            // Destroy entire mesh when a certain threshold is exceeded.
            if (fragmentsGraph.anchoredFragments.Count < fragmentsGraph.initialAnchoredCount / 2)
            {
                foreach (GameObject fragment in fragmentsGraph.graph.GetNodes())
                {
                    //fragmentsGraph.anchoredFragments.Remove(fragment);
                    DestroyFragment(fragment, hitPoint, radius, force);

                    // Ensure the current function operation does not take a lot of time
                    // in a single frame.
                    if (stopWatch.ElapsedMilliseconds > Constants.MaxTimeMs)
                    {
                        Debug.Log("Explosion Yield in destroying entire mesh");
                        yield return null;
                        stopWatch.Restart();
                    }
                }
                fragmentsGraph.graph = null;
                fragmentsGraph.anchoredFragments = null;
                fragmentsGraph.initialAnchoredCount = 0;
            }
            else
            {
                // Use flood fill to detect isolated islands of fragments.
                Queue<GameObject> toVisit = new Queue<GameObject>();
                HashSet<GameObject> visited = new HashSet<GameObject>();

                foreach (GameObject anchoredFragmnt in fragmentsGraph.anchoredFragments)
                {
                    toVisit.Enqueue(anchoredFragmnt);
                }

                while (toVisit.Count > 0)
                {
                    GameObject fragment = toVisit.Dequeue();
                    visited.Add(fragment);

                    if (fragmentsGraph.graph.ContainsNode(fragment))
                    {
                        foreach (GameObject neighbour in fragmentsGraph.graph.GetNeighbours(fragment).Keys)
                        {
                            if (!visited.Contains(neighbour))
                            {
                                toVisit.Enqueue(neighbour);
                            }
                        }
                    }

                    // Ensure the current function operation does not take a lot of time
                    // in a single frame.
                    if (stopWatch.ElapsedMilliseconds > Constants.MaxTimeMs)
                    {
                        Debug.Log("Explosion Yield in flood fill");
                        yield return null;
                        stopWatch.Restart();
                    }
                }

                // Find isolated islands of fragments and destroy all fragments from them.
                HashSet<GameObject> toRemove = new HashSet<GameObject>();

                foreach (GameObject fragment in fragmentsGraph.graph.GetNodes())
                {
                    if (!visited.Contains(fragment))
                    {
                        toRemove.Add(fragment);
                    }

                    // Ensure the current function operation does not take a lot of time
                    // in a single frame.
                    if (stopWatch.ElapsedMilliseconds > Constants.MaxTimeMs)
                    {
                        Debug.Log("Explosion Yield in finding islands");
                        yield return null;
                        stopWatch.Restart();
                    }
                }

                foreach (GameObject fragment in toRemove)
                {
                    fragmentsGraph.graph.RemoveNode(fragment);
                    fragmentsGraph.anchoredFragments.Remove(fragment);
                    DestroyFragment(fragment, hitPoint, radius, force);
                }
            }
        }

        if (returnedStats != null)
        {
            returnedStats.isDone = true;
            returnedStats.fragments = fragmentsExploded;
            returnedStats.fragmentedGameObject = null;
            returnedStats.destroyedAll = destroyedAll;
        }
    }


    /// <summary>
    /// Given an immovable fragment, destroy it by simulating an explosion. The explosion will be
    /// considered as a sphere. The fragment will be given a rigid body and will be moved accordingly.
    /// </summary>
    /// <param name="fragment">Immovable fragment to be destroyed.</param>
    /// <param name="hitPoint">Explosion center.</param>
    /// <param name="radius">Explosion radius (must be positive).</param>
    /// <param name="force">Explosion force (must be positive).</param>
    private static void DestroyFragment(GameObject fragment, Vector3 hitPoint, float radius, float force, bool immidiateHingeBreak = false)
    {
        if (fragment == null) throw new System.ArgumentNullException("fragment");
        if (!fragment.CompareTag(Constants.FrozenFragmentStr))
            throw new System.ArgumentException("Fragment is not tagged frozen");
        if (radius <= 0.0f) throw new System.ArgumentException("Radius is not greater than 0.0");
        if (force <= 0.0f) throw new System.ArgumentException("Force is not greater than 0.0");

        fragment.name = Constants.MovingFragmentStr + fragment.name.Substring(Constants.FrozenFragmentStr.Length);
        fragment.tag = Constants.MovingFragmentStr;
        fragment.layer = LayerMask.NameToLayer(Constants.MovingFragmentStr);
        
        Rigidbody rb = fragment.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = fragment.AddComponent<Rigidbody>();
        }
        rb.angularDrag = 0.0f;
        rb.drag = 0.0f;
        rb.AddExplosionForce(force, hitPoint, radius);
        Object.Destroy(fragment.GetComponent<JointBreakListener>());

        
        if (immidiateHingeBreak)
        {
            HingeJoint[] hingeJoints = fragment.GetComponents<HingeJoint>();
            foreach (HingeJoint hingeJoint in hingeJoints)
            {
                GameObject neighbour = hingeJoint.connectedBody.gameObject;
                Debug.Log($"Checking hinge {fragment.name} -> {neighbour.name}");
                HingeJoint[] neighHingeJoints = neighbour.GetComponents<HingeJoint>();
                foreach (HingeJoint neighHingeJoint in neighHingeJoints)
                {
                    //Debug.Log($"\t Checking {neighbour.name}'s hinge with {neighHingeJoint.connectedBody.name}");
                    if (neighHingeJoint.connectedBody.gameObject == fragment)
                    {
                        Debug.Log($"\t\t Destroyed hinge {neighHingeJoint.gameObject.name} -> {fragment.name}");
                        Object.DestroyImmediate(neighHingeJoint);
                        break;
                    }
                }
                Object.DestroyImmediate(hingeJoint);
            }
        }
        

        rb.isKinematic = false;
        fragment.transform.parent = null;
        Object.Destroy(fragment, Random.Range(Constants.FragmentMinDestroyDelay, Constants.FragmentMaxDestroyDelay));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyTransforms(Transform dst, Transform src)
    {
        dst.position = src.position;
        dst.rotation = src.rotation;
        dst.localPosition = src.localPosition;
        dst.localRotation = src.localRotation;
        dst.localScale = src.localScale;
        dst.tag = src.tag;
    }


    /// <summary>
    /// Given a triangle, subdivide it in multiple polygons using Voronoi diagrams.
    /// </summary>
    /// <param name="v0">First vertex of the triangle.</param>
    /// <param name="v1">Second vertex of the triangle.</param>
    /// <param name="v2">Third vertex of the triangle.</param>
    /// <param name="fragmentsProperties">Properties of the resulted fragments.</param>
    /// <returns>A list with all generated fragments' meshes. If the function failes to generate
    /// a fragment, it will the site of the fragment as a placeholder.</returns>
    private static List<GameObject> GenerateVoronoiFragments(
        Vertex v0, Vertex v1, Vertex v2, FragmentsProperties fragmentsProperties, bool areaInsteadOfSites = false)
    {
        if (fragmentsProperties == null) throw new System.ArgumentNullException("fragmentsProperties");

        float area = 0.5f * Vector3.Cross(v0.position - v1.position, v0.position - v2.position).magnitude;
        //Debug.Log($"Area {area}");
        //Debug.Log($"Max Area: {fragmentsProperties.maxArea}");

        // The Voronoi API suffers from innacurate floating-point approximations and provides erroneous
        // results when dealing with small numbers. A current solution to this problem is to upscale
        // the whole mesh, do the calculations and then downscale the result to the original scale.
        Vector3 scale = new Vector3(
            Constants.VoronoiScale, Constants.VoronoiScale, Constants.VoronoiScale
        );
        Vector3 reverseScale = new Vector3(
            1.0f / Constants.VoronoiScale, 1.0f / Constants.VoronoiScale, 1.0f / Constants.VoronoiScale
        );

        v0.position.Scale(scale);
        v1.position.Scale(scale);
        v2.position.Scale(scale);

        // The Voronoi API requires 2D points while the trianlge is in 3D space. In order to reduce
        // one dimension, all points must have the same z coordinate. Thus, rotate the triangle such
        // that its normal will become (0, 0, 1).
        Vector3 center = (v0.position + v1.position + v2.position) / 3.0f;
        Vector3 triangleNorm = Vector3.Cross(v1.position - v0.position, v2.position - v0.position).normalized;
        Quaternion rotation = Quaternion.FromToRotation(triangleNorm, Vector3.forward);
        Quaternion reverseRotation = Quaternion.FromToRotation(Vector3.forward, triangleNorm);
        Vector3 p0rot = (rotation * (v0.position - center) + center);
        Vector3 p1rot = (rotation * (v1.position - center) + center);
        Vector3 p2rot = (rotation * (v2.position - center) + center);
        float z = p0rot.z;

        // Try to make all fragments have similar area
        int sitesNum = areaInsteadOfSites ? Math.Max(3, (int)(area / fragmentsProperties.maxArea)) : fragmentsProperties.sitesPerTriangle;//Math.Max(3, (int)(area / fragmentsProperties.maxArea));
        //Debug.Log($"Sites num: {sitesNum}");
        Vector2[] sites = new Vector2[sitesNum];

        // Generate random points inside the triangle. These will be the sites passed to the Voronoi API.
        for (int i = 0; i < sites.Length; ++i)
        {
            Vector3 p = Vector3.zero;
            int triesLeft = 50;
            while (triesLeft > 0)
            {
                float r1 = Random.Range(0.1f, 0.9f);
                float r2 = Random.Range(0.1f, 0.9f);

                p = (float)(1.0f - Math.Sqrt(r1)) * p0rot +
                    (float)(Math.Sqrt(r1) * (1.0f - r2)) * p1rot +
                    (float)(r2 * Math.Sqrt(r1)) * p2rot;
                if (PointInTriangle(p0rot, p1rot, p2rot, p))
                {
                    break;
                }
                Debug.Log($"Bad point {p:F5}\n" +
                    $" inside triangle, ({p0rot:F5}, {p1rot:F5}, {p2rot:F5})\n" +
                    $"{triesLeft} tries left");
                --triesLeft;
            }
            sites[i] = p;
        }

        // Calculate the Voronoi diagram containing the given sites
        VoronoiCalculator calc = new VoronoiCalculator();
        VoronoiClipper clip = new VoronoiClipper();
        VoronoiDiagram diagram = calc.CalculateDiagram(sites);
        Vector2[] triangleClipper = new Vector2[3]
        {
            new Vector2(p0rot.x, p0rot.y),
            new Vector2(p1rot.x, p1rot.y),
            new Vector2(p2rot.x, p2rot.y)
        };
        List<Vector2> clipped = new List<Vector2>(); 
        List<GameObject> fragments = new List<GameObject>(sites.Length);

        // Generate a mesh for each site's polygon
        for (int i = 0; i < sites.Length; ++i)
        {
            clipped.Clear();
            clip.ClipSite(diagram, triangleClipper, i, ref clipped);

            if (clipped.Count > 0)
            {
                // Rotate the points back to their original rotation
                Vector3[] originalSpacePoints = new Vector3[clipped.Count];
                for (int j = 0; j < clipped.Count; ++j)
                {
                    Vector3 v = new Vector3(clipped[j].x, clipped[j].y, z);
                    originalSpacePoints[j] = reverseRotation * (v - center) + center;
                }

                Vertex[] vertices = CalculateNormalsAndUVs(v0, v1, v2, originalSpacePoints);

                // Revert the upscaling and scale to original object scale
                for (int j = 0; j < clipped.Count; ++j)
                {
                    vertices[j].position.Scale(reverseScale);
                }

                float thickness = Random.Range(fragmentsProperties.minThickness, fragmentsProperties.maxThickness);
                fragments.Add(PolygonToFrustum(vertices, thickness));
            }
        }
        return fragments;
    }


    /// <summary>
    /// Given a triangle and an array of points inside the triangle, interpolate the normal and
    /// uv coordinates of each inside point using baricentric coordinates.
    /// The function assumes the points are inside the triangle, no further checks are performed.
    /// </summary>
    /// <param name="v0">First vertex of the triangle.</param>
    /// <param name="v1">Second vertex of the triangle.</param>
    /// <param name="v2">Third vertex of the triangle.</param>
    /// <param name="points">Array of points inside the given triangle.</param>
    /// <returns>Array of vertices containing their positions, normals and uv coordinates.</returns>
    private static Vertex[] CalculateNormalsAndUVs(Vertex v0, Vertex v1, Vertex v2, Vector3[] points)
    {
        if (points == null) throw new System.ArgumentNullException("points");

        Vertex[] vertices = new Vertex[points.Length];

        for (int i = 0; i < points.Length; ++i)
        {
            // Calculate vectors from point p to vertices v1, v2 and v3
            Vector3 f0 = v0.position - points[i];
            Vector3 f1 = v1.position - points[i];
            Vector3 f2 = v2.position - points[i];

            // Calculate the areas and factors (order of parameters doesn't matter)
            float area = Vector3.Cross(v0.position - v1.position, v0.position - v2.position).magnitude;
            float fact0 = Vector3.Cross(f1, f2).magnitude / area;
            float fact1 = Vector3.Cross(f2, f0).magnitude / area;
            float fact2 = Vector3.Cross(f0, f1).magnitude / area;

            vertices[i].position = points[i];
            vertices[i].normal = v0.normal * fact0 + v1.normal * fact1 + v2.normal * fact2;
            vertices[i].uv     = v0.uv     * fact0 + v1.uv     * fact1 + v2.uv     * fact2;
        }
        return vertices;
    }


    ///<summary>
    /// Determine whether a point P is inside the triangle ABC. Note, this function
    /// assumes that P is coplanar with the triangle.
    ///</summary>
    ///<returns>True if the point is inside, false if it is not.</returns>
    private static bool PointInTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 P)
    {
        // Prepare barycentric variables
        Vector3 u = B - A;
        Vector3 v = C - A;
        Vector3 w = P - A;

        Vector3 vCrossW = Vector3.Cross(v, w);
        Vector3 vCrossU = Vector3.Cross(v, u);

        // Test sign of r
        if (Vector3.Dot(vCrossW, vCrossU) < 0)
            return false;

        Vector3 uCrossW = Vector3.Cross(u, w);
        Vector3 uCrossV = Vector3.Cross(u, v);

        // Test sign of t
        if (Vector3.Dot(uCrossW, uCrossV) < 0)
            return false;

        // At this point, we know that r and t and both > 0.
        // Therefore, as long as their sum is <= 1, each must be less <= 1
        float denom = uCrossV.magnitude;
        float r = vCrossW.magnitude / denom;
        float t = uCrossW.magnitude / denom;

        return (r + t < 1);
    }


    /// <summary>
    /// Given an array of vertices of a 2D polygon, create a frustum as a game object. The front face
    /// of the frustum will be the untouched polygon, the back face will be a downscaled version of
    /// the polygon (this helps to prevent z-fighting effects) and the side faces will be regular
    /// parallelograms.
    /// </summary>
    /// <param name="vertices">Polygon's vertices.</param>
    /// <param name="thickness">Frustum's thickness.</param>
    /// <returns>A game object with the generated frustum centerd in (0, 0, 0).</returns>
    private static GameObject PolygonToFrustum(Vertex[] vertices, float thickness)
    {
        if (vertices == null) throw new System.ArgumentNullException("vertices");
 
        int count = vertices.Length;
        Vector3[] verts = new Vector3[6 * count];
        Vector3[] norms = new Vector3[6 * count];
        Vector2[] uv = new Vector2[6 * count];
        int[] tris = new int[3 * 4 * (count - 1)];

        Vector3 center = Vector3.zero;
        Vector3 normal = Vector3.zero;
        for (int i = 0; i < count; ++i)
        {
            center += vertices[i].position;
            normal += vertices[i].normal;
        }
        center /= count;
        normal = normal.normalized;

        int vi = 0, ni = 0, ti = 0, ui = 0;

        // Front face
        for (int i = 0; i < count; ++i)
        {
            verts[vi++] = vertices[i].position;
            norms[ni++] = vertices[i].normal;
            uv[ui++] = vertices[i].uv;
        }
        for (int i = 1; i < count - 1; ++i)
        {
            tris[ti++] = 0;
            tris[ti++] = i;
            tris[ti++] = i + 1;
        }

        // Back face
        float contraction = Random.Range(Constants.MinContraction, Constants.MaxContraction);
        for (int i = 0; i < count; ++i)
        {
            Vector3 point = vertices[i].position - vertices[i].normal * thickness;
            point = Vector3.Lerp(point, center, contraction);
            verts[vi++] = point;
            norms[ni++] = -normal;
            uv[ui++] = vertices[i].uv;
        }
        for (int i = 1; i < count - 1; ++i)
        {
            tris[ti++] = count;
            tris[ti++] = count + i + 1;
            tris[ti++] = count + i;
        }

        // Side faces
        for (int i = 0; i < count; ++i)
        {
            int iNext = (i + 1) % count;

            Vector3 v0, v1, v2;
            verts[vi++] = v0 = verts[i];
            verts[vi++] = v1 = verts[count + i];
            verts[vi++] = v2 = verts[iNext];
            verts[vi++] = verts[count + iNext];

            Vector3 norm = Vector3.Cross(v1 - v0, v2 - v0);
            norms[ni++] = norm;
            norms[ni++] = norm;
            norms[ni++] = norm;
            norms[ni++] = norm;

            uv[ui++] = vertices[i].uv;
            uv[ui++] = Vector2.one - vertices[i].uv;
            uv[ui++] = vertices[iNext].uv;
            uv[ui++] = Vector2.one - vertices[iNext].uv;
        }
        for (int i = 0; i < count; ++i)
        {
            int si = 2 * count + 4 * i;

            tris[ti++] = si;
            tris[ti++] = si + 1;
            tris[ti++] = si + 2;

            tris[ti++] = si + 1;
            tris[ti++] = si + 3;
            tris[ti++] = si + 2;
        }

        // Translate the vertices so that the center of the mesh will become (0, 0, 0)
        center -= normal * thickness / 2;
        for (int i = 0; i < verts.Length; ++i)
        {
            verts[i] -= center;
        }

        Debug.Assert(ti == tris.Length);
        Debug.Assert(vi == verts.Length);

        Mesh mesh =  new Mesh
        {
            vertices = verts,
            normals = norms,
            triangles = tris,
            uv = uv
        };

        GameObject go = new GameObject();
        go.AddComponent<MeshFilter>().mesh = mesh;
        go.transform.localPosition = center;

        return go;
    }


    /// <summary>
    /// Upscale a mesh by moving its vertices farther away from its center of gravity.
    /// </summary>
    /// <param name="mesh">Mesh object to be upscaled.</param>
    /// <returns></returns>
    private static Mesh ScaleMesh(Mesh mesh, float scale)
    {
        if (mesh == null) throw new System.ArgumentNullException("mesh");

        Vector3 center = Vector3.zero;
        Vector3[] vertices = mesh.vertices;

        foreach (Vector3 v in vertices)
        {
            center += v;
        }
        center /= vertices.Length;

        for (int i = 0; i < vertices.Length; ++i)
        {
            vertices[i] = (vertices[i] - center) * scale;
        }
        return new Mesh
        {
            vertices = vertices,
            normals = mesh.normals,
            triangles = mesh.triangles,
            uv = mesh.uv
        };
    }


    private static float FragmentVolume(GameObject fragment)
    {
        if (fragment == null) throw new System.ArgumentNullException("fragment");
        if (!IsFragmented(fragment)) throw new System.ArgumentException("Game object is not a valid fragment");
        MeshFilter meshFilter = fragment.GetComponent<MeshFilter>();
        if (meshFilter == null) throw new System.ArgumentException("Game object does not have a mesh filter attached to it.");
        if (meshFilter.mesh == null) throw new System.ArgumentException("Game object's mesh filter does not have a mesh.");

        Mesh mesh = meshFilter.mesh;
        Vector3[] points = mesh.vertices;
        int baseStartIdx = 0;
        int baseEndIdx = points.Length / 6;
        int topStartIdx = points.Length / 6;
        int topEndIdx = points.Length / 3;

        Vector3 baseCenter = Vector3.zero;
        for (int i = baseStartIdx; i < baseEndIdx; ++i)
        {
            baseCenter += points[i];
        }
        baseCenter /= baseEndIdx - 1 - baseStartIdx;

        Vector3 topCenter = Vector3.zero;
        for (int i = topStartIdx; i < topEndIdx; ++i)
        {
            topCenter += points[i];
        }
        topCenter /= topEndIdx - 1 - topStartIdx;

        float baseArea = 0.0f;
        for (int i = baseStartIdx + 1; i < baseEndIdx - 1; ++i)
        {
            baseArea += Vector3.Cross(points[baseStartIdx] - points[i],
                                      points[baseStartIdx] - points[i + 1]).magnitude;
        }
        baseArea *= 0.5f;

        float topArea = 0.0f;
        for (int i = topStartIdx + 1; i < topEndIdx - 1; ++i)
        {
            topArea += Vector3.Cross(points[topStartIdx] - points[i],
                                     points[topStartIdx] - points[i + 1]).magnitude;
        }
        topArea *= 0.5f;

        float thickness = (topCenter - baseCenter).magnitude;
        float bigDistance = (points[baseStartIdx] - baseCenter).magnitude;
        float smallDistance = (points[topStartIdx] - topCenter).magnitude;
        float remainingThickness = thickness / (bigDistance - smallDistance);

        return baseArea * (thickness + remainingThickness) / 3.0f - topArea * remainingThickness / 3.0f;
    }


    private static Vector3 FragmentCenter(GameObject go)
    {
        if (go == null) throw new System.ArgumentNullException("go");
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null) throw new System.NullReferenceException("GameObject does not have a MeshFilter attached to it");
        Mesh m = mf.mesh;
        if (m == null) throw new System.NullReferenceException("MeshFilter does not have a Mesh attached to it");

        Vector3 center = Vector3.zero;
        foreach (Vector3 v in m.vertices)
        {
            center += v;
        }
        center /= m.vertexCount;

        return center;
    }
}
