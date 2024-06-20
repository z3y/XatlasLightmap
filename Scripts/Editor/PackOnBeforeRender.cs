using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace z3y
{
    public static class PackOnBeforeRender
    {
        [InitializeOnLoadMethod]
        public static void RegisterPackOnBeforeRender()
        {
            ftRenderLightmap.OnPreFullRender += PackOpenScene;
        }

        public static void PackOpenScene(object sender, EventArgs e)
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
                instances[i].Execute(false, true);
            }

        }
    }
}