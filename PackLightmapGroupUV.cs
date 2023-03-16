#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
public class PackLightmapGroupUV : MonoBehaviour
{
    public GameObject[] rootObjects;
    public bool forceUpdate = false;
    public bool clearStream = true;

    public int lightmapSize = 1024;
    public int padding = 2;

    [SerializeField] public List<Mesh> meshCache = new List<Mesh>();



    private void OnValidate()
    {
        if (!forceUpdate)
        {
            return;
        }


        var meshes = new List<Mesh>();
        GetActiveTransformsWithRenderers(rootObjects, out List<MeshRenderer> renderers, out List<MeshFilter> filters, out List<GameObject> objects);

        if (meshCache.Count == 0 || meshCache.Count != objects.Count)
        {



            for (int i = 0; i < objects.Count; i++)
            {
                var m_Renderer = renderers[i];

                var mf = filters[i];
                var stream = m_Renderer.additionalVertexStreams;
                var sm = mf.sharedMesh;


                if (clearStream)
                {
                    m_Renderer.additionalVertexStreams = null;
                    continue;
                }

                Vector2[] lightmapUV;
                var scale = m_Renderer.scaleInLightmap;

                var area = CalculateArea(sm);
                scale *= Mathf.Sqrt(area);

                lightmapUV = new Vector2[sm.uv2.Length];

                for (int j = 0; j < lightmapUV.Length; j++)
                {
                    lightmapUV[j] = sm.uv2[j] * scale;
                }


                stream = new Mesh
                {
                    vertices = sm.vertices,
                    normals = sm.normals,
                    uv = sm.uv,
                    uv2 = lightmapUV,
                    triangles = sm.triangles,
                    tangents = sm.tangents
                };

                var originalData = new Mesh
                {
                    vertices = sm.vertices,
                    uv = sm.uv,
                    uv2 = sm.uv2
                };


                meshes.Add(stream);

            }

            if (clearStream)
            {
                return;
            }


            z3y.xatlas.PackLightmap(meshes.ToArray(), padding, lightmapSize);

            meshCache = new List<Mesh>(meshes.Count);
            meshCache.AddRange(meshes);
        }


        for (int i = 0; i < meshCache.Count; i++)
        {
            var m_Renderer = renderers[i];
            var mf = filters[i];
            meshCache[i].UploadMeshData(false);
            if (meshCache[i].vertices.Length != mf.sharedMesh.vertices.Length)
            {
                Debug.LogError("Vertex count not the same + " + mf.sharedMesh.name + " " + meshes[i].vertices.Length + " original: " + mf.sharedMesh.vertices.Length);
            }
            m_Renderer.additionalVertexStreams = meshCache[i];
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

    private float CalculateArea(Mesh mesh)
    {
        var verts = mesh.vertices;
        float area = 0;

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
                area += Vector3.Cross(v2 - v1, v3 - v1).magnitude;
            }
        }

        return area;
    }
}
#endif
