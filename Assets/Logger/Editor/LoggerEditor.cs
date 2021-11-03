using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Logger))]
public class LoggerEditor : Editor
{
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Disconnect"))
        {
            Logger.Disconnect();
        }
    }


    [MenuItem("Assets/Create/Logger")]
    public static void CreateLogger()
    {
        if (Logger.Instance)
        {
            Debug.Log("Already existed");
            return;
        }
        var instance = CreateInstance<Logger>();
        AssetDatabase.CreateAsset(instance, "Assets/Logger.asset");
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(instance));
        Logger.Instance = instance;
    }
}
