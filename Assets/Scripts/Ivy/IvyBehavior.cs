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
            //refreshInterval = 0.7f;//0.7f;
            //probability = 0.5f;
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

                /*
                Debug.Log($"Delta Time: {Time.deltaTime}");
                if (Time.deltaTime > 0.05f)
                {
                    updatesToCalm = 10;
                    refreshInterval = 1.0f;
                }
                else
                {
                    updatesToCalm--;

                    if (updatesToCalm == 0)
                    {
                        refreshInterval = 0.4f;
                    }
                }
                */
            }

            /*
            if (Time.time > Constants.DestroyerBuildMinStart / 2 && Time.time < Constants.DestroyerBuildMinStart - 3)
            {
                refreshInterval *= 1.2f;
                Debug.Log($"Ivy: Refresh Interval now: {refreshInterval}");
            }
            else if (Time.time > Constants.DestroyerBuildMinStart - 3 && Time.time < Constants.DestroyerBuildMinStart)
            {
                refreshInterval *= 1.5f;
                Debug.Log($"Ivy: Refresh Interval now: {refreshInterval}");
            }
            else if (Time.time > Constants.DestroyerBuildMinStart && Time.time < Constants.DestroyerBuildMinStart + 3)
            {
                refreshInterval *= 1.5f;
                Debug.Log($"Ivy: Refresh Interval now: {refreshInterval}");
            }
            else if (Time.time > Constants.DestroyerBuildMaxStart + 3 && Time.time < 100.0f)
            {
                refreshInterval *= 1.2f;
                Debug.Log($"Ivy: Refresh Interval now: {refreshInterval}");
            }
            else if (Time.time > 150.0f)
            {
                refreshInterval *= 4.0f;
                Debug.Log($"Ivy: Refresh Interval now: {refreshInterval}");
            }
            */

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
            /*
            float dx = maxCoords.x - minCoords.x;
            float dz = maxCoords.z - minCoords.z;

            // First line
            int points = Random.Range(5, 8);
            for (int p = 0; p < points; ++p)
            {
                GameObject go = new GameObject($"MinZ{p}");
                go.transform.parent = gameObject.transform;
                go.transform.position = new Vector3(
                    Mathf.Lerp(minCoords.x, maxCoords.x, (float)(p) / (points - 1)) + dx / (2 * points) * Random.Range(-0.3f, 0.3f),
                    minCoords.y,
                    minCoords.z
                );
                roots.Add((go, Vector3.back));
            }

            points = Random.Range(5, 8);
            for (int p = 0; p < points; ++p)
            {
                GameObject go = new GameObject($"MaxZ{p}");
                go.transform.parent = gameObject.transform;
                go.transform.position = new Vector3(
                    Mathf.Lerp(minCoords.x, maxCoords.x, (float)(p) / (points - 1)) + dx / (2 * points) * Random.Range(-0.3f, 0.3f),
                    minCoords.y,
                    maxCoords.z
                );
                roots.Add((go, Vector3.forward));
            }

            points = Random.Range(4, 6);
            for (int p = 0; p < points; ++p)
            {
                GameObject go = new GameObject($"MinX{p}");
                go.transform.parent = gameObject.transform;
                go.transform.position = new Vector3(
                    minCoords.x,
                    minCoords.y,
                    Mathf.Lerp(minCoords.z, maxCoords.z, (float)(p) / (points - 1)) + dz / (2 * points) * Random.Range(-0.3f, 0.3f)
                );
                roots.Add((go, Vector3.left));
            }

            points = Random.Range(4, 6);
            for (int p = 0; p < points; ++p)
            {
                GameObject go = new GameObject($"MaxX{p}");
                go.transform.parent = gameObject.transform;
                go.transform.position = new Vector3(
                    maxCoords.x,
                    minCoords.y,
                    Mathf.Lerp(minCoords.z, maxCoords.z, (float)(p) / (points - 1)) + dz / (2 * points) * Random.Range(-0.3f, 0.3f)
                );
                roots.Add((go, Vector3.right));
            }
            */
            return roots;
        }
    }
}