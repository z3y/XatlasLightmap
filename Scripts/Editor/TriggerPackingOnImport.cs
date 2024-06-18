/*using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using z3y;

public class TriggerPackingOnImport : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var scene = EditorSceneManager.GetActiveScene();

        if (scene == null)
        {
            return;
        }


        var rootGameObjects = scene.GetRootGameObjects();
        var instances = new List<XatlasLightmapPacker>();
        foreach (var gameObject in rootGameObjects)
        {
            var xatlasLightmapPackers = gameObject.GetComponentsInChildren<XatlasLightmapPacker>(false);
            if (xatlasLightmapPackers is null || xatlasLightmapPackers.Length == 0)
            {
                continue;
            }
            instances.AddRange(xatlasLightmapPackers);
        }

        for (int i = 0; i < instances.Count; i++)
        {
            instances[i].Execute(true, false);
        }


    }
}
*/