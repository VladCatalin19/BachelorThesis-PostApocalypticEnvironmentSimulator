using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ivy
{
    [HelpURL("https://github.com/radiatoryang/hedera/wiki")]
    public class IvyBehavior : MonoBehaviour {

    public List<IvyGraph> ivyGraphs = new List<IvyGraph>();
    public bool generateMeshDuringGrowth = true, enableGrowthSim = true;
    public bool showProfileFoldout;
    public IvyProfileAsset profileAsset;
    public Color debugColor = Color.yellow;

    private float lastRefreshTime = 0.0f;
    public float refreshInterval;
    public float probability;

    private Vector3 minCoords = new Vector3(-3.4f, 0.0076f, -2.5f);
    private Vector3 maxCoords = new Vector3(+3.4f, 0.0076f, +2.5f);

    private void Start()
    {
        Time.timeScale = 1;
        IvyCore _ = new IvyCore();

        foreach ((Vector3 position, Vector3 normal) in GetRoots())
        {
            //Debug.Log($"This position: {transform.position}, Root position: {root.transform.position}");
            IvyGraph ivyGraph = IvyCore.SeedNewIvyGraph(profileAsset.ivyProfile, position, Vector3.up, normal, transform, true);
            ivyGraph.isGrowing = true;
            ivyGraphs.Add(ivyGraph);

            float branchPercentage = Mathf.Clamp(ivyGraph.roots[0].nodes.Last().cS / profileAsset.ivyProfile.maxLength, 0f, 0.38f);
            int branchCount = Mathf.FloorToInt(profileAsset.ivyProfile.maxBranchesTotal * branchPercentage * profileAsset.ivyProfile.branchingProbability);
            for (int b = 0; b < branchCount; b++)
            {
                IvyCore.ForceRandomIvyBranch(ivyGraph, profileAsset.ivyProfile);
            }
        }

            //Debug.Log($"Mask: {LayerMask.GetMask("Default", "UserLayerB")}");
    }


    private void Update()
    {
        if (Time.timeSinceLevelLoad > lastRefreshTime + refreshInterval)
        {
            //Debug.Log($"Time: {Time.time} Last Refresh Time: {lastRefreshTime} Refresh Interval: {refreshInterval}");
            lastRefreshTime = Time.timeSinceLevelLoad;
            IvyCore.Update(this, probability);
            refreshInterval += Time.deltaTime * 0.002f;
            //Debug.Log($"Refresh Interval: {refreshInterval}");
        }

        private List<(Vector3, Vector3)> GetRoots()
        {
            List<(Vector3, Vector3)> roots = new List<(Vector3, Vector3)>();
            foreach (Transform child in transform)
            {
                roots.Add((child.position, child.forward));
            }
            return roots;
        }
    }
}
