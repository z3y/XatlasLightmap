#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace z3y
{
    [ExecuteInEditMode]
    public class XatlasLightmapPacker : MonoBehaviour
    {
        public GameObject[] rootObjects;
        public bool forceUpdate = false;
        public bool clearStream = true;
        public bool bruteForce = false;

        public int lightmapSize = 1024;
        public int padding = 2;

        [SerializeField] public LightmapMeshData[] meshCache;

        [Serializable]
        public struct LightmapMeshData
        {
            public LightmapMeshData(Vector2[] uv)
            {
                lightmapUV = uv;
            }
            public Vector2[] lightmapUV;
        }

        public static List<GameObject> instances = new List<GameObject>();

        public void OnValidate()
        {
            if (!instances.Contains(gameObject))
            {
                instances.Add(gameObject);
            }

            if (!forceUpdate)
            {
                return;
            }

            var meshes = new List<Mesh>();
            GetActiveTransformsWithRenderers(rootObjects, out List<MeshRenderer> renderers, out List<MeshFilter> filters, out List<GameObject> objects);

            if (meshCache == null || meshCache.Length == 0 || meshCache.Length != objects.Count)
            {



                for (int i = 0; i < objects.Count; i++)
                {
                    var m_Renderer = renderers[i];

                    var mf = filters[i];
                    var sm = mf.sharedMesh;


                    if (clearStream)
                    {
                        m_Renderer.additionalVertexStreams = null;
                        continue;
                    }

                    Vector2[] lightmapUV;
                    var scale = m_Renderer.scaleInLightmap;


                    var area = CalculateArea(sm, objects[i].transform);
                    scale *= area;

                    lightmapUV = new Vector2[sm.uv2.Length];

                    for (int j = 0; j < lightmapUV.Length; j++)
                    {
                        lightmapUV[j] = sm.uv2[j] * scale;
                    }


                    var stream = new Mesh
                    {
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


                z3y.xatlas.PackLightmap(meshes.ToArray(), padding, lightmapSize, bruteForce);

                meshCache = new LightmapMeshData[meshes.Count];
                for (int k = 0; k < meshCache.Length; k++)
                {
                    var m = meshes[k];
                    m.UploadMeshData(true);
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
                }

                var avs = m_Renderer.additionalVertexStreams;
                var sm = mf.sharedMesh;

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
        }

        private void GetActiveTransformsWithRenderers(GameObject[] rootObjs, out List<MeshRenderer> renderers, out List<MeshFilter> filters, out List<GameObject> obs)
        {

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
                if (!o.activeInHierarchy || !o.isStatic)
                {
                    continue;
                }

                var r = root.GetComponent<MeshRenderer>();
                var f = root.GetComponent<MeshFilter>();

                if (!f || !r)
                {
                    continue;
                }

                obs.Add(o);
                renderers.Add(r);
                filters.Add(f);
            }
        }

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

                    var v1 = verts[indexA]; // should be transformed position
                    var v2 = verts[indexB];
                    var v3 = verts[indexC];
                    v1 = t.TransformPoint(v1);
                    v2 = t.TransformPoint(v2);
                    v3 = t.TransformPoint(v3);

                    area += Vector3.Cross(v2 - v1, v3 - v1).magnitude;

                    var u1 = uvs[indexA];
                    var u2 = uvs[indexB];
                    var u3 = uvs[indexC];
                    uvArea += Vector3.Cross(u2 - u1, u3 - u1).magnitude;
                }
            }

            area = Mathf.Sqrt(area) / Mathf.Sqrt(uvArea);

            return area;
        }
    }
    public class RemoveObjectOnBuild : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

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

            //Debug.Log(instances.Count);
            for (int i = 0; i < instances.Count; i++)
            {
                instances[i].OnValidate();
                Object.DestroyImmediate(instances[i].gameObject);
            }
        }
    }
}
#endif
