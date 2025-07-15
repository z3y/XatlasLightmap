#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

namespace z3y
{
    public class Xatlas
    {
        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern float PackLightmap(IntPtr mesh, int meshLength, bool blockAlign, int padding, int resolution, bool bruteForce, int maxChartSize, float texelSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct XatlasMesh
        {
            public IntPtr vertices;
            public IntPtr uvs;
            public IntPtr indices;

            public int vertexCount;
            public int indexCount;
            public int submeshIndex;
        }
        public static void PackLightmap(Mesh[] meshes, int padding, int resolution, bool bruteForce)
        {
            var xatlasMeshes = new List<XatlasMesh>();

            var handles = new List<GCHandle>();

            try
            {
                var uvs = new List<Vector2[]>();
                for (int i = 0; i < meshes.Length; i++)
                {
                    var mesh = meshes[i];

                    var xMesh = new XatlasMesh();
                    xMesh.vertexCount = mesh.vertexCount;
                    GCHandle vHandle = GCHandle.Alloc(mesh.vertices, GCHandleType.Pinned);
                    handles.Add(vHandle);
                    var vPtr = vHandle.AddrOfPinnedObject();
                    xMesh.vertices = vPtr;

                    var lightmapUV = mesh.uv2 ?? mesh.uv;
                    GCHandle uvHandle = GCHandle.Alloc(lightmapUV, GCHandleType.Pinned);
                    handles.Add(uvHandle);
                    var uvPtr = uvHandle.AddrOfPinnedObject();
                    xMesh.uvs = uvPtr;

                    for (int j = 0; j < mesh.subMeshCount; j++)
                    {
                        var indices = mesh.GetIndices(j);

                        GCHandle iHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
                        handles.Add(iHandle);
                        var iPtr = iHandle.AddrOfPinnedObject();
                        xMesh.indices = iPtr;
                        xMesh.indexCount = (int)mesh.GetIndexCount(j);
                        xMesh.submeshIndex = j;

                        xatlasMeshes.Add(xMesh);
                    }
                    uvs.Add(lightmapUV);

                }

                var meshesArray = xatlasMeshes.ToArray();
                var handle = GCHandle.Alloc(meshesArray, GCHandleType.Pinned);
                handles.Add(handle);
                var ptr = handle.AddrOfPinnedObject();

                if (PackLightmap(ptr, meshesArray.Length, true, padding, resolution, bruteForce, 1024, 0) == 0)
                {
                    Debug.Log("Lightmap Packed");
                    for (int k = 0; k < meshes.Length; k++)
                    {
                        meshes[k].uv2 = uvs[k];
                    }
                }

                
            }
            finally
            {
                foreach (var h in handles)
                {
                    h.Free();
                }
            }

        }
    }
}
#endif