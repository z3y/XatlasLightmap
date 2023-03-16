#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static UnityEngine.InputSystem.Editor.InputActionCodeGenerator;

namespace z3y
{

    public class xatlas
    {
        //#define UV_HINT

        public static List<Vector2> newUVBuffer;
        public static List<int> newXrefBuffer;

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern System.IntPtr xatlasCreateAtlas();

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern int xatlasAddMeshExt(System.IntPtr atlas, int vertexCount, System.IntPtr positions, System.IntPtr normals, System.IntPtr uv, int indexCount, int[] indices32);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern int xatlasAddUVMesh(System.IntPtr atlas, int vertexCount, System.IntPtr uv, int indexCount, int[] indices32, bool allowRotate);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern void xatlasParametrizeExt(System.IntPtr atlas, bool useInputMeshUVs);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern void xatlasPackExt(System.IntPtr atlas, int resolution, int texelSize, int padding, bool bruteForce, int maxChartSize, bool blockAlign);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern void xatlasNormalize(System.IntPtr atlas, int[] atlasSizes, bool preferDensity);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern int xatlasGetAtlasCount(System.IntPtr atlas);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern int xatlasGetAtlasIndex(System.IntPtr atlas, int meshIndex, int chartIndex);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern int xatlasGetVertexCount(System.IntPtr atlas, int meshIndex);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern int xatlasGetIndexCount(System.IntPtr atlas, int meshIndex);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern void xatlasGetData(System.IntPtr atlas, int meshIndex, System.IntPtr outUV, System.IntPtr outRef, System.IntPtr outIndices);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern int xatlasClear(System.IntPtr atlas);

        [DllImport("xatlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern void xatlasOnlyPackCharts(System.IntPtr atlas);

        static T[] FillAtrribute<T>(List<int> xrefArray, T[] origArray)
        {
            if (origArray == null || origArray.Length == 0) return origArray;

            var arr = new T[xrefArray.Count];
            for (int i = 0; i < xrefArray.Count; i++)
            {
                int xref = xrefArray[i];
                arr[i] = origArray[xref];
            }
            return arr;

        }

        public static double GetTime()
        {
            return (System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond) / 1000.0;
        }

        public static void Unwrap(Mesh m, UnwrapParam uparams, bool normalize = false)
        {
            int padding = (int)(uparams.packMargin * 1024);

            newUVBuffer = null;
            newXrefBuffer = null;

            var t = GetTime();

            var positions = m.vertices;
            var normals = m.normals;
            var existingUV = m.uv;
            var handlePos = GCHandle.Alloc(positions, GCHandleType.Pinned);
            var handleNorm = GCHandle.Alloc(normals, GCHandleType.Pinned);
            var handleUV = GCHandle.Alloc(existingUV, GCHandleType.Pinned);
            int err = 0;

            var atlas = xatlasCreateAtlas();

            try
            {
                var pointerPos = handlePos.AddrOfPinnedObject();
                var pointerNorm = handleNorm.AddrOfPinnedObject();

#if false
                var pointerUV = handleUV.AddrOfPinnedObject();
#else
            var pointerUV = (System.IntPtr)0;
#endif

                for (int i = 0; i < m.subMeshCount; i++)
                {
                    err = xatlasAddMeshExt(atlas, m.vertexCount, pointerPos, pointerNorm, pointerUV, (int)m.GetIndexCount(i), m.GetIndices(i));
                    if (err == 1)
                    {
                        Debug.LogError("xatlas::AddMesh: indices are out of range");
                    }
                    else if (err == 2)
                    {
                        Debug.LogError("xatlas::AddMesh: index count is incorrect");
                    }
                    else if (err != 0)
                    {
                        Debug.LogError("xatlas::AddMesh: unknown error");
                    }
                    if (err != 0) break;
                }
                if (err == 0)
                {

                    xatlasParametrizeExt(atlas, uparams.hardAngle > 179);
                    int res = 1024;
                    xatlasPackExt(atlas, 4096, 0, padding, false, res, true);//, true);
                    //xatlasPackExt(atlas, 4096, 0, 0, 1024, padding, false, true);//, true);
                }
            }
            finally
            {
                if (handlePos.IsAllocated) handlePos.Free();
                if (handleNorm.IsAllocated) handleNorm.Free();
                if (handleUV.IsAllocated) handleUV.Free();
            }
            if (err != 0)
            {
                xatlasClear(atlas);
                return;
            }

            Debug.Log("xatlas time: " + (GetTime() - t));
            t = GetTime();

            var indexBuffers = new List<int[]>();

            newUVBuffer = new List<Vector2>();
            newXrefBuffer = new List<int>();
            while (newUVBuffer.Count < m.vertexCount)
            {
                newUVBuffer.Add(new Vector2(-100, -100));
                newXrefBuffer.Add(0);
            }

            //if (normalize)
                xatlasNormalize(atlas, null, false);

            // Collect UVs/xrefs/indices
            for (int i = 0; i < m.subMeshCount; i++)
            {
                // Get data from xatlas
                int newVertCount = xatlasGetVertexCount(atlas, i);
                int indexCount = xatlasGetIndexCount(atlas, i); // should be unchanged

                var uvBuffer = new Vector2[newVertCount];
                var xrefBuffer = new int[newVertCount];
                var indexBuffer = new int[indexCount];

                var handleT = GCHandle.Alloc(uvBuffer, GCHandleType.Pinned);
                var handleX = GCHandle.Alloc(xrefBuffer, GCHandleType.Pinned);
                var handleI = GCHandle.Alloc(indexBuffer, GCHandleType.Pinned);

                try
                {
                    var pointerT = handleT.AddrOfPinnedObject();
                    var pointerX = handleX.AddrOfPinnedObject();
                    var pointerI = handleI.AddrOfPinnedObject();

                    xatlasGetData(atlas, i, pointerT, pointerX, pointerI);
                }
                finally
                {
                    if (handleT.IsAllocated) handleT.Free();
                    if (handleX.IsAllocated) handleX.Free();
                    if (handleI.IsAllocated) handleI.Free();
                }

                // Generate new UV buffer and xatlas->final index mappings
                var xatlasIndexToNewIndex = new int[newVertCount];
                for (int j = 0; j < newVertCount; j++)
                {
                    int xref = xrefBuffer[j];
                    Vector2 uv = uvBuffer[j];

                    if (newUVBuffer[xref].x < 0)
                    {
                        // first xref encounter gets UV
                        xatlasIndexToNewIndex[j] = xref;
                        newUVBuffer[xref] = uv;
                        newXrefBuffer[xref] = xref;
                    }
                    else if (newUVBuffer[xref].x == uv.x && newUVBuffer[xref].y == uv.y)
                    {
                        // vertex already added
                        xatlasIndexToNewIndex[j] = xref;
                    }
                    else
                    {
                        // duplicate vertex
                        xatlasIndexToNewIndex[j] = newUVBuffer.Count;
                        newUVBuffer.Add(uv);
                        newXrefBuffer.Add(xref);
                    }
                }

                // Generate final index buffer
                for (int j = 0; j < indexCount; j++)
                {
                    indexBuffer[j] = xatlasIndexToNewIndex[indexBuffer[j]];
                }
                indexBuffers.Add(indexBuffer);
            }

            int vertCount = m.vertexCount;

            bool origIs16bit = true;
#if UNITY_2017_3_OR_NEWER
            origIs16bit = m.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16;
#endif
            bool is32bit = newUVBuffer.Count >= 65000;//0xFFFF;
            if (is32bit && origIs16bit)
            {
                Debug.LogError("Unwrap failed: original mesh (" + m.name + ") has 16 bit indices, but unwrapped requires 32 bit.");
                return;
            }

            // Duplicate attributes
            //if (newXrefBuffer.Count > m.vertexCount) // commented because can be also swapped around
            {
                m.vertices = FillAtrribute<Vector3>(newXrefBuffer, positions);
                m.normals = FillAtrribute<Vector3>(newXrefBuffer, normals);
                m.boneWeights = FillAtrribute<BoneWeight>(newXrefBuffer, m.boneWeights);
                m.colors32 = FillAtrribute<Color32>(newXrefBuffer, m.colors32);
                m.tangents = FillAtrribute<Vector4>(newXrefBuffer, m.tangents);
                m.uv = FillAtrribute<Vector2>(newXrefBuffer, m.uv);
                m.uv3 = FillAtrribute<Vector2>(newXrefBuffer, m.uv3);
                m.uv4 = FillAtrribute<Vector2>(newXrefBuffer, m.uv4);
#if UNITY_2018_2_OR_NEWER
                m.uv5 = FillAtrribute<Vector2>(newXrefBuffer, m.uv5);
                m.uv6 = FillAtrribute<Vector2>(newXrefBuffer, m.uv6);
                m.uv7 = FillAtrribute<Vector2>(newXrefBuffer, m.uv7);
                m.uv8 = FillAtrribute<Vector2>(newXrefBuffer, m.uv8);
#endif
            }

            m.uv2 = newUVBuffer.ToArray();


            // Set indices
            for (int i = 0; i < m.subMeshCount; i++)
            {
                m.SetTriangles(indexBuffers[i], i);
            }

            xatlasClear(atlas);
        }
        public static void PackLightmap(Mesh[] meshes, int padding, int res, bool bruteForce)
        {

            newUVBuffer = null;
            newXrefBuffer = null;

            var t = GetTime();
            var atlas = xatlasCreateAtlas();

            bool error = false;
            for (int j = 0; j < meshes.Length; j++)
            {
                var m = meshes[j];
                var positions = m.vertices;
                var normals = m.normals;
                var existingUV = m.uv2 == null ? m.uv : m.uv2;
                var handlePos = GCHandle.Alloc(positions, GCHandleType.Pinned);
                var handleNorm = GCHandle.Alloc(normals, GCHandleType.Pinned);
                var handleUV = GCHandle.Alloc(existingUV, GCHandleType.Pinned);
                int err = 0;


                try
                {
                    var pointerPos = handlePos.AddrOfPinnedObject();
                    var pointerNorm = handleNorm.AddrOfPinnedObject();

#if true
                    var pointerUV = handleUV.AddrOfPinnedObject();
#else
            var pointerUV = (System.IntPtr)0;
#endif

                    for (int i = 0; i < m.subMeshCount; i++)
                    {
                        //err = xatlasAddMeshExt(atlas, m.vertexCount, pointerPos, pointerNorm, pointerUV, (int)m.GetIndexCount(i), m.GetIndices(i));
                        err = xatlasAddUVMesh(atlas, m.vertexCount, pointerUV, (int)m.GetIndexCount(i), m.GetIndices(i), true);
                        if (err == 1)
                        {
                            Debug.LogError("xatlas::AddMesh: indices are out of range");
                        }
                        else if (err == 2)
                        {
                            Debug.LogError("xatlas::AddMesh: index count is incorrect");
                        }
                        else if (err != 0)
                        {
                            Debug.LogError("xatlas::AddMesh: unknown error");
                        }
                        if (err != 0) break;
                    }
                    if (err == 0)
                    {

                    }
                }
                finally
                {
                    if (handlePos.IsAllocated) handlePos.Free();
                    if (handleNorm.IsAllocated) handleNorm.Free();
                    if (handleUV.IsAllocated) handleUV.Free();
                }
                if (err != 0)
                {
                    error = true;
                    xatlasClear(atlas);
                    return;
                }
            }

            if (error)
            {
                Debug.LogError("xatlas failed");
                return;
            }

            //xatlasOnlyPackCharts(atlas);
            //xatlasParametrizeExt(atlas, true);

            xatlasPackExt(atlas, res, 0, padding, bruteForce, res, true);//, true);
            xatlasNormalize(atlas, null, true);

            Debug.Log("xatlas time: " + (GetTime() - t));
            t = GetTime();

            int meshIndex = 0;
            for (int k = 0; k < meshes.Length; k++)
            {
                var m = meshes[k];

                var indexBuffers = new List<int[]>();

                newUVBuffer = new List<Vector2>();
                newXrefBuffer = new List<int>();
                while (newUVBuffer.Count < m.vertexCount)
                {
                    newUVBuffer.Add(new Vector2(-100, -100));
                    newXrefBuffer.Add(0);
                }


            
                // Collect UVs/xrefs/indices
                for (int ii = 0; ii < m.subMeshCount; ii++)
                {
                    int i = meshIndex;
                    meshIndex++;


                    // Get data from xatlas
                    int newVertCount = xatlasGetVertexCount(atlas, i);
                    int indexCount = xatlasGetIndexCount(atlas, i); // should be unchanged

                    var uvBuffer = new Vector2[newVertCount];
                    var xrefBuffer = new int[newVertCount];
                    var indexBuffer = new int[indexCount];

                    var handleT = GCHandle.Alloc(uvBuffer, GCHandleType.Pinned);
                    var handleX = GCHandle.Alloc(xrefBuffer, GCHandleType.Pinned);
                    var handleI = GCHandle.Alloc(indexBuffer, GCHandleType.Pinned);

                    try
                    {
                        var pointerT = handleT.AddrOfPinnedObject();
                        var pointerX = handleX.AddrOfPinnedObject();
                        var pointerI = handleI.AddrOfPinnedObject();

                        xatlasGetData(atlas, i, pointerT, pointerX, pointerI);
                    }
                    finally
                    {
                        if (handleT.IsAllocated) handleT.Free();
                        if (handleX.IsAllocated) handleX.Free();
                        if (handleI.IsAllocated) handleI.Free();
                    }

                    // Generate new UV buffer and xatlas->final index mappings
                    var xatlasIndexToNewIndex = new int[newVertCount];
                    for (int j = 0; j < newVertCount; j++)
                    {
                        int xref = xrefBuffer[j];
                        Vector2 uv = uvBuffer[j];

                        if (newUVBuffer[xref].x < 0)
                        {
                            // first xref encounter gets UV
                            xatlasIndexToNewIndex[j] = xref;
                            newUVBuffer[xref] = uv;
                            newXrefBuffer[xref] = xref;
                        }
                        else if (newUVBuffer[xref].x == uv.x && newUVBuffer[xref].y == uv.y)
                        {
                            // vertex already added
                            xatlasIndexToNewIndex[j] = xref;
                        }
                        else
                        {
                            // duplicate vertex
                            xatlasIndexToNewIndex[j] = newUVBuffer.Count;
                            newUVBuffer.Add(uv);
                            newXrefBuffer.Add(xref);
                        }
                    }

                    // Generate final index buffer
                    for (int j = 0; j < indexCount; j++)
                    {
                        indexBuffer[j] = xatlasIndexToNewIndex[indexBuffer[j]];
                    }
                    indexBuffers.Add(indexBuffer);
                }

                int vertCount = m.vertexCount;

                bool origIs16bit = true;
#if UNITY_2017_3_OR_NEWER
                origIs16bit = m.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16;
#endif
                bool is32bit = newUVBuffer.Count >= 65000;//0xFFFF;
                if (is32bit && origIs16bit)
                {
                    Debug.LogError("Unwrap failed: original mesh (" + m.name + ") has 16 bit indices, but unwrapped requires 32 bit.");
                    return;
                }

                // Duplicate attributes
                //if (newXrefBuffer.Count > m.vertexCount) // commented because can be also swapped around
                {
                    m.vertices = FillAtrribute<Vector3>(newXrefBuffer, m.vertices);
                    m.normals = FillAtrribute<Vector3>(newXrefBuffer, m.normals);
                    m.boneWeights = FillAtrribute<BoneWeight>(newXrefBuffer, m.boneWeights);
                    m.colors32 = FillAtrribute<Color32>(newXrefBuffer, m.colors32);
                    m.tangents = FillAtrribute<Vector4>(newXrefBuffer, m.tangents);
                    m.uv = FillAtrribute<Vector2>(newXrefBuffer, m.uv);
                    m.uv3 = FillAtrribute<Vector2>(newXrefBuffer, m.uv3);
                    m.uv4 = FillAtrribute<Vector2>(newXrefBuffer, m.uv4);
#if UNITY_2018_2_OR_NEWER
                    m.uv5 = FillAtrribute<Vector2>(newXrefBuffer, m.uv5);
                    m.uv6 = FillAtrribute<Vector2>(newXrefBuffer, m.uv6);
                    m.uv7 = FillAtrribute<Vector2>(newXrefBuffer, m.uv7);
                    m.uv8 = FillAtrribute<Vector2>(newXrefBuffer, m.uv8);
#endif
                }

                m.uv2 = newUVBuffer.ToArray();


                //Set indices
                for (int i = 0; i < m.subMeshCount; i++)
                {
                    m.SetTriangles(indexBuffers[i], i);
                }
            }

            xatlasClear(atlas);
        }
    }
}
#endif
