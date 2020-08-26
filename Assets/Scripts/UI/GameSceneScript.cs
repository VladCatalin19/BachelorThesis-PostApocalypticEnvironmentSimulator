using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;
using System;

public class GameSceneScript : MonoBehaviour
{
    public RawImage loadingImage;

    public TMP_InputField timeScaleInput;
    public TextMeshProUGUI timeScaleText;

    public float timeScaleStep = 0.1f;
    private bool timeScaleSelected = false;

    public List<GameObject> roads;
    public GameObject trees;

    private void Awake()
    {
        FadingScript fs = gameObject.AddComponent<FadingScript>();
        fs.img = loadingImage;
        loadingImage.enabled = true;
        fs.nextSceneName = null;
        fs.StartAnimation(true);
    }

    private void Update()
    {
        if (!timeScaleSelected)
        {
            timeScaleInput.text = timeScaleText.text = string.Format("{0,5:0.0}", Time.timeScale);
        }

        //Debug.Log($"Time scale: {Time.timeScale}");
        //Debug.Log($"String format: {string.Format("{0,5:0.0}", Time.timeScale)}");
    }

    public void TimeScaleIncremet()
    {
        Time.timeScale += timeScaleStep;
        //localTimeScale += timeScaleStep;
    }

    public void TimeScaleDecrement()
    {
        Time.timeScale = Mathf.Max(Time.timeScale - timeScaleStep, 0.0f);
        //localTimeScale = Mathf.Max(localTimeScale - timeScaleStep, 0.0f);
    }

    public void OnSelectTimeScale()
    {
        timeScaleSelected = true;
    }

    public void OnDeseletTimeScale()
    {
        timeScaleSelected = false;
    }

    public void OnValueChange(string s)
    {
        Debug.Log("On Value Change");
        try
        {
            float timeScale = float.Parse(s);
            //localTimeScale = timeScale;
        }
        catch (System.Exception)
        {

        }
    }

    public void OnEnd(string s)
    {
        Debug.Log($"On End Edit {s}");
        s = timeScaleInput.text;
        try
        {
            float timeScale = float.Parse(s, new System.Globalization.CultureInfo("en-US").NumberFormat);
            //localTimeScale = timeScale;
            Time.timeScale = timeScale;
            Debug.Log("On End Edit Set Time Scale");
        }
        catch (System.Exception e)
        {
            Debug.Log($"On End Edit Exception {e}");
        }
        //timeScaleSelected = false;
    }

    public void SaveMesh()
    {
        string fileName = $"scene_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.fbx";
        string basePath = @"C:/Facultate/Unity/Licenta/Build/2020.06.18/Exports/";
        string fullPath = System.IO.Path.Combine(basePath, fileName);
        GameObject root = new GameObject("Temporary Root");

        Dictionary<GameObject, GameObject> treesParents = new Dictionary<GameObject, GameObject>();

        foreach (GameObject road in roads)
        {
            road.transform.SetParent(root.transform, false);
        }

        foreach (Transform treeGroup in trees.transform)
        {
            foreach (Transform tree in treeGroup)
            {
                if (tree.GetComponent<MeshRenderer>().enabled)
                {
                    treesParents[tree.gameObject] = tree.parent.gameObject;
                    tree.SetParent(root.transform, true);
                    break;
                }
            }
        }

        UnityFBXExporter.FBXExporter.ExportGameObjToFBX(root, fullPath, false, false);

        foreach (GameObject road in roads)
        {
            road.transform.SetParent(null, false);
        }

        foreach (KeyValuePair<GameObject, GameObject> pair in treesParents)
        {
            pair.Key.transform.SetParent(pair.Value.transform, true);
        }

        Destroy(root);
    }

    public void MainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("MenuScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
