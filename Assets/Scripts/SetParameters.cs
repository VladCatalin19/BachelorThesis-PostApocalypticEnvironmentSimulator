using Ivy;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class SetParameters : MonoBehaviour
{
    public enum IvyGrowingSpeed { none, slow, medium, fast, veryFast};
    public enum DestructionTime { soon, late };

    public int housesNum = 9;
    public IvyGrowingSpeed ivyGrowingSpeed = IvyGrowingSpeed.slow;
    public DestructionTime destructionStart = DestructionTime.soon;
    public bool treeGrowing = true;
    public int sitesPerTriangle;

    public List<GameObject> houses;

    // Start is called before the first frame update
    void Awake()
    {
        // Load values from menu or assume defaults
        if (PlayerPrefs.HasKey("HouseNumber") == false)
        {
            PlayerPrefs.SetInt("HouseNumber", 9);
            housesNum = 9;
        }
        else
        {
            housesNum = PlayerPrefs.GetInt("HouseNumber");
        }

        if (PlayerPrefs.HasKey("GrowTrees") == false)
        {
            PlayerPrefs.SetInt("GrowTrees", 1);
            treeGrowing = true;
        }
        else
        {
            treeGrowing = System.Convert.ToBoolean(PlayerPrefs.GetInt("GrowTrees"));
        }

        if (PlayerPrefs.HasKey("IvyGrowingSpeed") == false)
        {
            PlayerPrefs.SetInt("IvyGrowingSpeed", (int)IvyGrowingSpeed.medium);
            ivyGrowingSpeed = IvyGrowingSpeed.medium;
        }
        else
        {
            ivyGrowingSpeed = (IvyGrowingSpeed)PlayerPrefs.GetInt("IvyGrowingSpeed");
        }


        if (!PlayerPrefs.HasKey("SitesPerTriangle"))
        {
            PlayerPrefs.SetInt("SitesPerTriangle", 3);
            sitesPerTriangle = 3;
        }
        else
        {
            sitesPerTriangle = PlayerPrefs.GetInt("SitesPerTriangle");
        }

        if (!PlayerPrefs.HasKey("DestructionStart"))
        {
            PlayerPrefs.SetInt("DestructionStart", 0);
            destructionStart = DestructionTime.soon;
        }
        else
        {
            destructionStart = (DestructionTime)PlayerPrefs.GetInt("DestructionStart");
        }

        // Change number of houses in the scene
        housesNum = Mathf.Clamp(housesNum, 0, houses.Count);
        for (int i = houses.Count - 1; i >= housesNum; --i)
        {
            DestroyImmediate(houses[houses.Count - 1]);
            houses.RemoveAt(houses.Count - 1);
        }

        foreach (GameObject house in houses)
        {
            //house.transform.Find("Walls+Roof").gameObject.GetComponent<FragmentsProperties>().sitesPerTriangle = sitesPerTriangle;
            foreach (Transform child in house.transform)
            {
                if (child.name.StartsWith("House"))
                {
                    Transform grandchild = child.Find("Walls+Roof");
                    if (grandchild != null)
                    {
                        FragmentsProperties fp = grandchild.gameObject.GetComponent<FragmentsProperties>();
                        if (fp != null)
                        {
                            fp.sitesPerTriangle = sitesPerTriangle;
                        }
                    }
                }
            }
        }

        // Change how fast the ivy will grow in the scene
        foreach (IvyBehavior iv in FindObjectsOfType<IvyBehavior>())
        {
            switch(ivyGrowingSpeed)
            {
                case IvyGrowingSpeed.none:
                    iv.refreshInterval = 1000.0f;
                    iv.probability = 0.0f;
                    break;

                case IvyGrowingSpeed.slow:
                    iv.refreshInterval = 0.7f;
                    iv.probability = 0.5f;
                    break;

                case IvyGrowingSpeed.medium:
                    iv.refreshInterval = 0.5f;
                    iv.probability = 0.5f;
                    break;

                case IvyGrowingSpeed.fast:
                    iv.refreshInterval = 0.3f;
                    iv.probability = 0.5f;
                    break;

                case IvyGrowingSpeed.veryFast:
                    iv.refreshInterval = 0.1f;
                    iv.probability = 0.7f;
                    break;
            }
        }

        // Change whether tree will grow in the scene
        foreach (TreeGrowing t in FindObjectsOfType<TreeGrowing>())
        {
            t.isGrowing = treeGrowing;
        }

        switch(destructionStart)
        {
            case DestructionTime.soon:
                Constants.DestroyerMiscMinStart = 5.0f;
                Constants.DestroyerMiscMaxStart = 6.0f;

                Constants.DestroyerBuildMinStart = 10.0f;
                Constants.DestroyerBuildMaxStart = 15.0f;
                break;

            case DestructionTime.late:
                Constants.DestroyerMiscMinStart = 10.0f;
                Constants.DestroyerMiscMaxStart = 15.0f;

                Constants.DestroyerBuildMinStart = 40.0f;
                Constants.DestroyerBuildMaxStart = 50.0f;
                break;
        }
    }
}
