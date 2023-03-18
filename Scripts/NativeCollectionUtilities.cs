using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

/* 
    Utility for memcopying data from native to managed containers and vice versa.
    
    Todo: Is it possible to generic memcopy implementations, where Source, Dest : struct?
    If not, you know what to do: *** code generation ***
    
    This trick was originally learned from a tweet by @LotteMakesStuff
 */

public static class NativeCollectionUtilities {

    /* float2 <-> Vector2 */

    public static unsafe void CopyToManaged(NativeArray<float2> source, Vector2[] destination) {
        fixed (void* vertexArrayPointer = destination) {
            UnsafeUtility.MemCpy(
                vertexArrayPointer,
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(source),
                destination.Length * (long)UnsafeUtility.SizeOf<float2>());
        }
    }

    public static unsafe void CopyToNative(Vector2[] source, NativeArray<float2> destination) {
        if (source.Length != destination.Length) {
            throw new System.ArgumentException("Source length is not equal to destination length");
        }
        fixed (void* sourcePointer = source) {
            UnsafeUtility.MemCpy(
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(destination),
                sourcePointer,
                destination.Length * (long)UnsafeUtility.SizeOf<float2>());
        }
    }

    public static unsafe void CopyToNative(int[] source, NativeArray<int> destination) {
        if (source.Length != destination.Length) {
            throw new System.ArgumentException("Source length is not equal to destination length");
        }
        fixed (void* sourcePointer = source) {
            UnsafeUtility.MemCpy(
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(destination),
                sourcePointer,
                destination.Length * (long)UnsafeUtility.SizeOf<int>());
        }
    }

    /* float3 <-> Vector3 */

    public static unsafe void CopyToManaged(NativeArray<float3> source, Vector3[] destination) {
        fixed (void* vertexArrayPointer = destination) {
            UnsafeUtility.MemCpy(
                vertexArrayPointer,
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(source),
                destination.Length * (long)UnsafeUtility.SizeOf<float3>());
        }
    }

    public static unsafe void CopyToNative(Vector3[] source, NativeArray<float3> destination) {
        if (source.Length != destination.Length) {
            throw new System.ArgumentException("Source length is not equal to destination length");
        }
        fixed (void* sourcePointer = source) {
            UnsafeUtility.MemCpy(
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(destination),
                sourcePointer,
                destination.Length * (long)UnsafeUtility.SizeOf<float3>());
        }
    }

    /* float4 <-> Vector4 */

    public static unsafe void CopyToManaged(NativeArray<float4> source, Vector4[] destination) {
        fixed (void* vertexArrayPointer = destination) {
            UnsafeUtility.MemCpy(
                vertexArrayPointer,
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(source),
                destination.Length * (long)UnsafeUtility.SizeOf<float4>());
        }
    }

    public static unsafe void CopyToNative(Vector4[] source, NativeArray<float4> destination) {
        if (source.Length != destination.Length) {
            throw new System.ArgumentException("Source length is not equal to destination length");
        }
        fixed (void* sourcePointer = source) {
            UnsafeUtility.MemCpy(
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(destination),
                sourcePointer,
                destination.Length * (long)UnsafeUtility.SizeOf<float4>());
        }
    }

    /* float4 <-> Color */

    public static unsafe void CopyToManaged(NativeArray<float4> source, Color[] destination) {
        fixed (void* vertexArrayPointer = destination) {
            UnsafeUtility.MemCpy(
                vertexArrayPointer,
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(source),
                destination.Length * (long)UnsafeUtility.SizeOf<float4>());
        }
    }

    public static unsafe void CopyToNative(Color[] source, NativeArray<float4> destination) {
        if (source.Length != destination.Length) {
            throw new System.ArgumentException("Source length is not equal to destination length");
        }
        fixed (void* sourcePointer = source) {
            UnsafeUtility.MemCpy(
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(destination),
                sourcePointer,
                destination.Length * (long)UnsafeUtility.SizeOf<float4>());
        }
    }

    /* int <-> int */

    public static unsafe void CopyToManaged(NativeArray<int> source, int[] destination) {
        fixed (void* vertexArrayPointer = destination) {
            UnsafeUtility.MemCpy(
                vertexArrayPointer,
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(source),
                destination.Length * (long)UnsafeUtility.SizeOf<int>());
        }
    }

    /* float3 <-> Texture2D */

    public static void ToTexture2D(NativeArray<float3> screen, Texture2D tex, int2 resolution) {
        Color[] colors = new Color[screen.Length];

        for (int i = 0; i < screen.Length; i++) {
            var c = screen[i];
            colors[i] = new Color(c.x, c.y, c.z, 1f);
        }

        tex.SetPixels(0, 0, (int)resolution.x, (int)resolution.y, colors, 0);
        tex.Apply();
    }

    public static void SetToConstant(NativeSlice<float> values, float value) {
        for (int i = 0; i < values.Length; i++) {
            values[i] = value;
        }
    }

    public static void ExportImage(Texture2D texture, string folder) {
        var bytes = texture.EncodeToJPG(100);
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(folder, string.Format("render_{0}.png", System.DateTime.Now.ToFileTimeUtc())),
            bytes);
    }
}