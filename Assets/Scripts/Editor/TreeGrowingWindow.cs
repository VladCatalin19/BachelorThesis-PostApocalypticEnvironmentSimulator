using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TreeGrowingWindow : EditorWindow
{
    private Tree treeTemplate = null;
    private int treeCount = 1;

    [MenuItem("Tools/GenerateGrowth"), MenuItem("Window/Tree Growingr")]
    public static void ShowWindow()
    {
        GetWindowInstance();
    }

    private static TreeGrowingWindow GetWindowInstance()
    {
        var window = GetWindow<TreeGrowingWindow>("Tree Growingr");

#if UNITY_5_4_OR_NEWER
        window.titleContent = new GUIContent();
#else
        window.title = Constants.ASSET_NAME;
#endif

        window.Show();

        return window;
    }

    void OnGUI()
    {
        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("You can only generate trees while not in playmode!", MessageType.Info);
            return;
        }

        HandleDragDrop();

        treeTemplate = EditorGUILayout.ObjectField("Tree Template", treeTemplate, typeof(Tree), true) as Tree;
        treeCount = EditorGUILayout.IntSlider("Tree Count", treeCount, 1, 50);

        if (GUILayout.Button("Generate Trees"))
        {
            //Debug.Log("It Works!");
            GrowTree(treeTemplate, treeCount);
        }
    }

    private void HandleDragDrop()
    {
        var currentEvent = Event.current;
        var currentEventType = currentEvent.type;

        if (currentEventType == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            DragAndDrop.AcceptDrag();
        }
        if (currentEventType == EventType.DragPerform)
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Tree)
                {
                    treeTemplate = obj as Tree;
                }
                else if (obj is GameObject)
                {
                    treeTemplate = (obj as GameObject).GetComponent<Tree>();
                }
            }
        }
    }



    private static Tree GetAssetTree(Tree tree)
    {
        if (AssetDatabase.Contains(tree))
            return tree;

        GameObject go = tree.gameObject;
        GameObject asset = PrefabUtility.GetPrefabParent(go) as GameObject;
        if (asset == null)
            return null;

        return asset.GetComponent<Tree>();
    }

    public static void GrowTree(Tree template, int treeCount)
    {
        if (template == null)
            return;

        Debug.Log("Starting generation of " + treeCount + " trees.");

        template = GetAssetTree(template);

        if (!AssetDatabase.Contains(template))
        {
            Debug.LogError("The tree was not found in the AssetDatabase.", template);
            return;
        }

        string path = AssetDatabase.GetAssetPath(template);
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);

        string outputFolder = Path.Combine(dir, template.name + " Growths");
        if (!AssetDatabase.IsValidFolder(outputFolder))
            AssetDatabase.CreateFolder(dir, template.name + " Growths");

        var templateSerialized = new SerializedObject(template.data);

        Material[] materials = template.GetComponent<MeshRenderer>().sharedMaterials;
        Material barkmat = templateSerialized.FindProperty("optimizedSolidMaterial").objectReferenceValue as Material;
        if (barkmat == null)
        {
            Debug.LogError("bark material not found!");
            return;
        }
        Material leafmat = templateSerialized.FindProperty("optimizedCutoutMaterial").objectReferenceValue as Material;
        if (leafmat == null)
        {
            Debug.LogError("leaf material not found");
            return;
        }

        List<Tree> generatedTrees = new List<Tree>();
        for (int i = 0; i < treeCount; i++)
        {
            string outFile = name + "_" + i + ext;
            string outPath = Path.Combine(outputFolder, outFile);

            bool success = AssetDatabase.CopyAsset(path, outPath);
            AssetDatabase.Refresh();
            if (!success)
            {
                Debug.LogError("Could not copy the tree from " + path + " to " + outPath);
                return;
            }

            AssetDatabase.ImportAsset(outPath);
            Tree newTree = AssetDatabase.LoadAssetAtPath(outPath, typeof(Tree)) as Tree;

            SerializedObject newTreeSerialized = new SerializedObject(newTree.data);
            Material newTreeBark = newTreeSerialized.FindProperty("optimizedSolidMaterial").objectReferenceValue as Material;
            Material newTreeLeaf = newTreeSerialized.FindProperty("optimizedCutoutMaterial").objectReferenceValue as Material;

            {
                if (newTreeBark != null)
                    Object.DestroyImmediate(newTreeBark, true);
                if (newTreeLeaf != null)
                    Object.DestroyImmediate(newTreeLeaf, true);

                newTreeSerialized.FindProperty("optimizedSolidMaterial").objectReferenceValue = barkmat;
                newTreeSerialized.FindProperty("optimizedCutoutMaterial").objectReferenceValue = leafmat;

                newTree.GetComponent<MeshRenderer>().sharedMaterials = materials;

                AssetDatabase.DeleteAsset(outputFolder + "/" + name + "_" + i + "_Textures");
            }

            AssetDatabase.SaveAssets();


            //int randomSeed = Random.Range(0, 9999999);
            //newTreeSerialized.FindProperty("root.seed").intValue = randomSeed;
            int index = newTreeSerialized.FindProperty("branchGroups").arraySize - 1;
            //GetArrayElementAtIndex
            Debug.Log($"{newTreeSerialized.FindProperty("branchGroups").GetArrayElementAtIndex(index)}");


            newTreeSerialized.ApplyModifiedProperties();
            MethodInfo meth = newTree.data.GetType().GetMethod("UpdateMesh", new[] { typeof(Matrix4x4), typeof(Material[]).MakeByRefType() });
            object[] arguments = new object[] { newTree.transform.worldToLocalMatrix, null };
            meth.Invoke(newTree.data, arguments);

            generatedTrees.Add(newTree);
        }
    }
}
