#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace z3y
{
    // since static batching already creates a copy of the same mesh for every instance
    // there is no reason to use lightmap tiling and offset when we can just set different uv2 for each mesh renderer and pack them very efficiently
    // this gets merged by static batching creating no additional cost
    [ExecuteInEditMode]
    public class XatlasLightmapPacker : MonoBehaviour
    {
        //public GameObject[] rootObjects; // the renderers here would be on the same lightmap group with no uv adjustments (original uv)
        public BakeryLightmapGroup lightmapGroup;
        public bool autoUpdateUVs = false;
        public bool bruteForce = false;

        public bool ignoreScaleInLightmap = false;

        //public int lightmapSize = 1024;
        public int LightmapSize() => lightmapGroup == null ? 1024 : lightmapGroup.resolution;
        public int padding = 2;

        [Serializable]
        public struct LightmapMeshData
        {
            public LightmapMeshData(Vector2[] uv)
            {
                lightmapUV = uv;
            }
            public Vector2[] lightmapUV;
        }

        public GameObject[] GetRootGameObjectsFromGroup()
        {
            var objects = new List<GameObject>();
            var selectors = FindObjectsOfType<BakeryLightmapGroupSelector>();


            foreach (var selector in selectors )
            {
                if (selector.lmgroupAsset != lightmapGroup || !selector.enabled)
                {
                    continue;
                }

                objects.Add(selector.gameObject);
            }

            if (selectors.Length == 0)
            {
                objects.Add(gameObject);
            }


            return objects.ToArray();
        }

        // This should probably be done somewhere else, but im not sure exactly when does unity clear the additionalVertexStreams
        // it seems to happen on scene load, entering play mode and reload

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!autoUpdateUVs)
            {
                ClearVertexStreams();
                return;
            }
            Execute(false, false);
        }

        public void ClearVertexStreams()
        {
            bool lastAutoUpdateUVs = autoUpdateUVs;
            autoUpdateUVs = false;
            Execute(true, false);
            autoUpdateUVs = lastAutoUpdateUVs;
        }

        public void Execute(bool clearStream, bool regenerateData, bool directlyToUv2 = false)
        {
            var meshes = new List<Mesh>();
            var rootObjects = GetRootGameObjectsFromGroup();
            GetActiveTransformsWithRenderers(rootObjects, out List<MeshRenderer> renderers, out List<MeshFilter> filters, out List<GameObject> objects);

            LightmapMeshData[] meshCache = null;
            if (!regenerateData)
            {
                TryReadData(ref meshCache);
            }
            bool needsSave = meshCache == null;

            if (clearStream || meshCache == null || meshCache.Length == 0 || meshCache.Length != objects.Count)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    var m_Renderer = renderers[i];

                    var mf = filters[i];
                    var sm = mf.sharedMesh;


                    if (clearStream)
                    {
                        m_Renderer.additionalVertexStreams = null;
                        EditorUtility.SetDirty(m_Renderer);
                        continue;
                    }


                    var scale = ignoreScaleInLightmap ? 1f : m_Renderer.scaleInLightmap;
                    int length = sm.vertices.Length;


                    Vector2[] lightmapUV = new Vector2[length];
#if true
                    NativeArray<float3> verts = new NativeArray<float3>(length, Allocator.TempJob);
                    NativeArray<float2> uvs = new NativeArray<float2>(length, Allocator.TempJob);
                    NativeCollectionUtilities.CopyToNative(sm.vertices, verts);
                    NativeCollectionUtilities.CopyToNative(sm.HasVertexAttribute(VertexAttribute.TexCoord1) ? sm.uv2 : sm.uv, uvs);
                    NativeArray<float> result = new NativeArray<float>(2, Allocator.TempJob);
                    try
                    {
                        Matrix4x4 modelMatrix = objects[i].transform.localToWorldMatrix;

                        result[0] = 0;
                        result[1] = 0;

                        for (int j = 0; j < sm.subMeshCount; j++)
                        {
                            var indicies = new NativeArray<int>(sm.GetIndices(j), Allocator.TempJob);

                            var areaMultiplier = new CalculateChartsAreaMultiplierJob(verts, uvs, modelMatrix, indicies, result);
                            areaMultiplier.Run(indicies.Length/3);

                            indicies.Dispose();
                        }
                        float area = result[0];
                        float uvArea = result[1];
                        float finalScale = (math.sqrt(area) / math.sqrt(uvArea)) * scale;

                        var scaleJob = new ScaleUVsJob(uvs, finalScale);
                        scaleJob.Run(uvs.Length);

                        NativeCollectionUtilities.CopyToManaged(uvs, lightmapUV);
                    }
                    finally
                    {
                        result.Dispose();
                        uvs.Dispose();
                        verts.Dispose();
                    }
#else
                    var area = CalculateArea(sm, objects[i].transform);
                    scale *= area;
                    var refUv = sm.uv2 ?? sm.uv;

                    for (int j = 0; j < lightmapUV.Length; j++)
                    {
                        lightmapUV[j] = refUv[j] * scale;
                    }

#endif
                    var stream = new Mesh
                    {
                        name = sm.name,
                        indexFormat = sm.indexFormat, // thanks haxy
                        vertices = sm.vertices,
                        normals = sm.normals,
                        uv = sm.uv,
                        uv2 = lightmapUV,
                        triangles = sm.triangles,
                        tangents = sm.tangents
                    };


                    meshes.Add(stream);

                }


                if (clearStream)
                {
                    return;
                }


                Xatlas.PackLightmap(meshes.ToArray(), padding, LightmapSize(), bruteForce);

                meshCache = new LightmapMeshData[meshes.Count];
                for (int k = 0; k < meshCache.Length; k++)
                {
                    var m = meshes[k];
                    //m.UploadMeshData(true);
                    meshCache[k] = new LightmapMeshData(m.uv2);
                    DestroyImmediate(m);
                }

            }


            for (int i = 0; i < meshCache.Length; i++)
            {
                var m_Renderer = renderers[i];
                var mf = filters[i];
                if (meshCache[i].lightmapUV.Length != mf.sharedMesh.vertices.Length)
                {
                    Debug.LogError("Vertex count not the same + " + mf.sharedMesh.name + " " + meshCache[i].lightmapUV.Length + " original: " + mf.sharedMesh.vertices.Length);
                    continue;
                }


                var sm = mf.sharedMesh;
                var avs = m_Renderer.additionalVertexStreams;
#if SLZ_BATCHING
                if (directlyToUv2)
                {
                    var newMesh = Instantiate(sm);
                    avs = newMesh;
                    mf.sharedMesh = newMesh;
                }
#endif

                // vertices have to be set to match the uv2 length
                // it can cause problems when editing and reimporting the geometry but thankfully its non destructive and setting them again or clearing streams fixes it
                // reimport should be detected and this data updated so the mesh doesnt look messed up
                if (avs == null)
                {
                    avs = new Mesh
                    {
                        vertices = mf.sharedMesh.vertices,
                        uv2 = meshCache[i].lightmapUV
                    };
                }
                else
                {
                    if (avs.vertices == null || avs.vertices.Length != sm.vertices.Length)
                    {
                        avs.vertices = mf.sharedMesh.vertices;
                    }
                    avs.uv2 = meshCache[i].lightmapUV;
                }

                m_Renderer.additionalVertexStreams = avs;
                avs.UploadMeshData(false);
            }

            if (needsSave)
            {
                WriteData(meshCache);
            }

        }

        public void RePackCharts()
        {
            bool lastAutoUpdateUVs = autoUpdateUVs;
            autoUpdateUVs = true;
            Execute(false, true);
            autoUpdateUVs = lastAutoUpdateUVs;
        }
        public void PackCharts()
        {
            Execute(false, false);
        }

        public void GetActiveTransformsWithRenderers(GameObject[] rootObjs, out List<MeshRenderer> renderers, out List<MeshFilter> filters, out List<GameObject> obs)
        {
            var infoMsg = new StringBuilder();

            var roots = new List<Transform>();

            for (int i = 0; i < rootObjs.Length; i++)
            {
                var o = rootObjs[i];
                roots.AddRange(o.GetComponentsInChildren<Transform>(false));
            }

            roots.Distinct();

            renderers = new List<MeshRenderer>();
            filters = new List<MeshFilter>();
            obs = new List<GameObject>();


            foreach (var root in roots)
            {
                var o = root.gameObject;
                if (!o.activeInHierarchy)
                {
                    continue;
                }

                if (!GameObjectUtility.GetStaticEditorFlags(o).HasFlag(StaticEditorFlags.ContributeGI))
                {
                    continue;
                }

                var objName = o.name;

                var r = root.GetComponent<MeshRenderer>();
                var f = root.GetComponent<MeshFilter>();

                if (!f || !r)
                {
                    continue;
                }

                if (r.receiveGI != ReceiveGI.Lightmaps)
                {
                    continue;
                }

                if (r.scaleInLightmap == 0)
                {
                    continue;
                }

                if (f.sharedMesh == null)
                {
                    //infoMsg.AppendLine($"{objName}: has no shared mesh");
                    continue;
                }



                var sm = f.sharedMesh;

                if (sm.vertices == null)
                {
                    infoMsg.AppendLine($"{objName}: mesh has no vertices");
                    continue;
                }

                if (sm.uv2 == null && sm.uv == null)
                {
                    infoMsg.AppendLine($"{objName}: mesh has no uvs");
                    continue;
                }

                var uv = sm.HasVertexAttribute(VertexAttribute.TexCoord1) ? sm.uv2 : sm.uv;

                if (uv.Length != sm.vertices.Length)
                {
                    infoMsg.AppendLine($"{objName}: uv length does not equal vertices length");
                    continue;
                }

                obs.Add(o);
                renderers.Add(r);
                filters.Add(f);
            }

            var msg = infoMsg.ToString();
            if (!string.IsNullOrEmpty(msg))
            {
                Debug.LogWarning(msg);
            }
        }

        //public RenderTexture rt;
        //public Material mt;
        //pr Material lmblur;
        [HideInInspector] public Texture2D lightmap;
        [Range(1,5)]
        [HideInInspector] public int radius = 2;

        public int PreprocessOrder => throw new NotImplementedException();

        public void GaussianPrefilter(Renderer[] renderers)
        {
            if ( lightmap is null)
            {
                return;
            }

            var rt = new RenderTexture(lightmap.width, lightmap.height, 0, RenderTextureFormat.R8);

            var lmblur = new Material(Shader.Find("Hidden/z3y/LMBlur"));
            var mt = new Material(Shader.Find("Hidden/z3y/UnwrapUV"));

            //RenderTexture.active = rt;
            var cmd = new CommandBuffer();
            cmd.SetRenderTarget(rt);
            //mt.SetPass(0);

            cmd.ClearRenderTarget(true, true, Color.black);

            for (int i = 0; i < renderers.Length; i++)
            {
                cmd.DrawRenderer(renderers[i], mt);
            }

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            lmblur.SetTexture("_MainTex", lightmap);
            lmblur.SetTexture("_Mask", rt);
            lmblur.SetFloat("_Radius", radius);
            float width = lightmap.width;
            float height = lightmap.height;
            lmblur.SetVector("_lightmapRes", new Vector4(1.0f / width, 1.0f / height, width, height));

            var lmrt = new RenderTexture(lightmap.width, lightmap.height, 0, RenderTextureFormat.ARGBFloat);
            var tex = new Texture2D(lmrt.width, lmrt.height, TextureFormat.RGBAFloat, false);

            RenderTexture.active = lmrt;
            Graphics.Blit(null, lmrt, lmblur);
            tex.ReadPixels(new Rect(0, 0, lmrt.width, lmrt.height), 0, 0);
            tex.Apply(false);

            RenderTexture.active = null;
            var bytes = tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
            //var bytes = tex.EncodeToPNG();
            var path = AssetDatabase.GetAssetPath(lightmap);
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);
        }

        // this is a bit slow
        private float CalculateArea(Mesh mesh, Transform t)
        {
            if (mesh.uv == null)
            {
                return 1f;
            }
            var verts = mesh.vertices;
            var uvs = mesh.uv2 ?? mesh.uv;
            float area = 0;
            float uvArea = 0;

            if (uvs.Length != mesh.vertices.Length)
            {
                return 1f;
            }

            for (int k = 0; k < mesh.subMeshCount; k++)
            {
                var indices = mesh.GetIndices(k);
                for (int j = 0; j < indices.Length; j += 3)
                {
                    var indexA = indices[j];
                    var indexB = indices[j + 1];
                    var indexC = indices[j + 2];

                    var v1 = verts[indexA];
                    var v2 = verts[indexB];
                    var v3 = verts[indexC];
                    v1 = t.TransformPoint(v1);
                    v2 = t.TransformPoint(v2);
                    v3 = t.TransformPoint(v3);

                    area += Vector3.Cross(v2 - v1, v3 - v1).magnitude;

                    var u1 = uvs[indexA];
                    var u2 = uvs[indexB];
                    var u3 = uvs[indexC];

                    var d = determinant(u1, u2, u3);
                    uvArea += math.abs(d);
                }
            }



            area = Mathf.Sqrt(area) / Mathf.Sqrt(uvArea);

            return area;
        }

        float determinant(float2 c, float2 c2, float2 c3)
        {
            float num = c2.y - c3.y;
            float num2 = c.y - c3.y;
            float num3 = c.y - c2.y;
            return c.x * num - c2.x * num2 + c3.x * num3;
        }

        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        private struct CalculateChartsAreaMultiplierJob : IJobParallelFor
        {
            public CalculateChartsAreaMultiplierJob(NativeArray<float3> verts, NativeArray<float2> uvs, Matrix4x4 modelMatrix, NativeArray<int> indices, NativeArray<float> result)
            {
                this.verts = verts;
                this.uvs = uvs;
                this.modelMatrix = modelMatrix;
                this.indices = indices;
                this.result = result;
            }

            public NativeArray<int> indices;
            public NativeArray<float3> verts;
            public NativeArray<float2> uvs;
            public float4x4 modelMatrix;

            public NativeArray<float> result; // length 2

            float determinant(float2 c, float2 c2, float2 c3)
            {
                float num = c2.y - c3.y;
                float num2 = c.y - c3.y;
                float num3 = c.y - c2.y;
                return c.x * num - c2.x * num2 + c3.x * num3;
            }

            public void Execute(int index)
            {
                int i = index * 3;
                var indexA = indices[i];
                var indexB = indices[i + 1];
                var indexC = indices[i + 2];

                var v1 = verts[indexA];
                var v2 = verts[indexB];
                var v3 = verts[indexC];
                v1 = math.transform(modelMatrix, v1);
                v2 = math.transform(modelMatrix, v2);
                v3 = math.transform(modelMatrix, v3);

                result[0] += math.length(math.cross(v2 - v1, v3 - v1));

                var u1 = uvs[indexA];
                var u2 = uvs[indexB];
                var u3 = uvs[indexC];

                var d = determinant(u1, u2, u3);
                result[1] += math.abs(d);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct ScaleUVsJob : IJobParallelFor
        {
            public ScaleUVsJob(NativeArray<float2> uvs, float scale)
            {
                this.uvs = uvs;
                this.scale = scale;
            }

            public NativeArray<float2> uvs;
            float scale;
            public void Execute(int index)
            {
                uvs[index] *= scale;
            }
        }

        private void WriteData(LightmapMeshData[] data)
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(m))
                {
                    w.Write(data.Length);
                    for (int i = 0; i < data.Length; i++)
                    {
                        var uv = data[i].lightmapUV;

                        w.Write(uv.Length);
                        for (int j = 0; j < uv.Length; j++)
                        {
                            w.Write(uv[j].x);
                            w.Write(uv[j].y);
                        }
                    }
                }
                var bytes = m.ToArray();

                File.WriteAllBytes(GetDataPath(), bytes);
            }
        }

        private void TryReadData(ref LightmapMeshData[] data)
        {
            var path = GetDataPath();
            if (!File.Exists(path))
            {
                return;
            }

            var bytes = File.ReadAllBytes(path);

            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    var totalLength = reader.ReadInt32();

                    var result = new LightmapMeshData[totalLength];

                    for (int i = 0; i < result.Length; i++)
                    {
                        var length = reader.ReadInt32();

                        var uvs = new Vector2[length];

                        for (int j = 0; j < uvs.Length; j++)
                        {
                            uvs[j].x = reader.ReadSingle();
                            uvs[j].y = reader.ReadSingle();
                        }

                        result[i].lightmapUV = uvs;
                    }

                    data = result;
                }
            }
        }

        private string GetDataPath()
        {
            string libraryPath = Path.Combine(Application.dataPath, "../Library/XatlasCache");

            if (!Directory.Exists(libraryPath))
            {
                Directory.CreateDirectory(libraryPath);
            }

            var idString = GlobalObjectId.GetGlobalObjectIdSlow(gameObject);
            var sceneGuid = AssetDatabase.AssetPathToGUID(gameObject.scene.path);

            return Path.Combine(libraryPath, idString.targetObjectId + sceneGuid);
        }

        /*bool IPreprocessCallbackBehaviour.OnPreprocess()
        {
            //PackCharts();
            return true;
        }*/
    }
    public class PackDataOnBuild : IProcessSceneWithReport
    {
        public int callbackOrder => -10000;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
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
                instances[i].Execute(false, false, true);
            }
        }
    }

    [CustomEditor(typeof(XatlasLightmapPacker))]
    [CanEditMultipleObjects]
    public class XatlasLightmapPackerEditor : Editor
    {
        bool _firstFrame = true;


        List<MeshFilter> _meshFilters;
        public override void OnInspectorGUI()
        {

            if (Application.isPlaying)
            {
                return;
            }

            DrawDefaultInspector();
            for (int i = 0; i < targets.Length; i++)
            {
                var t = targets[i];

                var packer = t as XatlasLightmapPacker;


                if (GUILayout.Button("Pack"))
                {
                    EditorUtility.SetDirty(packer.gameObject);
                    packer.RePackCharts();
                }
                if (GUILayout.Button("Clear"))
                {
                    EditorUtility.SetDirty(packer.gameObject);
                    packer.ClearVertexStreams();
                }
                /*if (GUILayout.Button("Gaussian Prefilter (wip)"))
                {
                    var meshes = new List<Mesh>();
                    packer.GetActiveTransformsWithRenderers(packer.GetRootGameObjectsFromGroup(), out List<MeshRenderer> renderers, out List<MeshFilter> filters, out List<GameObject> objects);
                    //var lightmap = renderers[0].lightmapIndex;
                    packer.GaussianPrefilter(renderers.ToArray());
                }*/

            }

            EditorGUILayout.Space();
            Warnings();

            _firstFrame = false;
        }

        // Mimics the normal map import warning - written by Orels1
        private static bool WarningBox(string message, string fixText = "Fix Now")
        {
            GUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));
            EditorGUILayout.LabelField(message, new GUIStyle(EditorStyles.label) { fontSize = 11, wordWrap = true });
            EditorGUILayout.BeginHorizontal(new GUIStyle() { alignment = TextAnchor.MiddleRight }, GUILayout.Height(24));
            EditorGUILayout.Space();
            var buttonPress = GUILayout.Button(fixText, new GUIStyle("button")
            {
                stretchWidth = false,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 8, 0, 0)
            }, GUILayout.Height(22));
            EditorGUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return buttonPress;
        }

        Vector2 _scrollPos;
        void Warnings()
        {
            var packer = target as XatlasLightmapPacker;
            if (packer.lightmapGroup == null)
            {
                return;
            }

            var storage = FindObjectOfType<ftLightmapsStorage>();
            if (storage != null)
            {
                if (!(storage.renderSettingsForceDisableUnwrapUVs || !storage.renderSettingsUnwrapUVs))
                {
                    EditorGUILayout.HelpBox("Disable Adjust UV Padding in bakery lightmap settings", MessageType.Warning);
                }
            }


            if (_firstFrame)
            {
                var rootObjects = packer.GetRootGameObjectsFromGroup();
                packer.GetActiveTransformsWithRenderers(rootObjects, out List<MeshRenderer> renderers, out List<MeshFilter> filters, out List<GameObject> objects);
                _meshFilters = filters;
            }

            var group = packer.lightmapGroup;

            if (group.mode != BakeryLightmapGroup.ftLMGroupMode.OriginalUV)
            {
                if (WarningBox("Lightmap Group mode not set to Original UV"))
                {
                    group.mode = BakeryLightmapGroup.ftLMGroupMode.OriginalUV;
                    EditorUtility.SetDirty(group);
                }
            }


            // this is cursed but how else do i know in advance if i should draw the scroll view lol
            bool hasWarnings = false;
            if (_meshFilters == null)
            {
                return;
            }
            for (int i = 0; i < _meshFilters.Count; i++)
            {
                var m = _meshFilters[i].sharedMesh;

                if (!m.HasVertexAttribute(VertexAttribute.TexCoord1))
                {
                    hasWarnings = true; break;
                }

                if (!m.isReadable)
                {
                    hasWarnings = true; break;
                }
            }

            if (hasWarnings)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
                for (int i = 0; i < _meshFilters.Count; i++)
                {
                    var m = _meshFilters[i].sharedMesh;

                    if (!m.HasVertexAttribute(VertexAttribute.TexCoord1))
                    {
                        if (WarningBox($"Mesh {m.name} has no UV2", "Select"))
                        {
                            EditorGUIUtility.PingObject(m);
                        }
                    }

                    if (!m.isReadable)
                    {
                        if (WarningBox($"Mesh {m.name} has no Read/Write enabled", "Select"))
                        {
                            EditorGUIUtility.PingObject(m);
                        }
                    }

                }
                EditorGUILayout.EndScrollView();
            }



        }
    }

    /*public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = UnityEngine.JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return UnityEngine.JsonUtility.ToJson(wrapper);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }*/
}
#endif