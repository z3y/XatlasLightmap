#include "xatlas.h"
#include <minmax.h>
#include <iostream>

extern "C"
{
	_declspec(dllexport) void* _cdecl xatlasCreateAtlas()
	{
		return (void*)xatlas::Create();
	}

	_declspec(dllexport) int _cdecl xatlasAddMeshExt(void* atlas,
		int vertexCount, float* positions, float* normals, float* uv,
		int indexCount, unsigned int* indices32)
	{
		xatlas::MeshDecl decl;
		decl.vertexCount = vertexCount;
		decl.vertexPositionData = (void*)positions;
		decl.vertexPositionStride = sizeof(float) * 3;
		decl.vertexNormalData = (void*)normals;
		decl.vertexNormalStride = sizeof(float) * 3;
		decl.vertexUvData = uv;
		if (uv != 0) decl.vertexUvStride = sizeof(float) * 2;
		decl.indexCount = indexCount;
		decl.indexData = (void*)indices32;
		decl.indexFormat = xatlas::IndexFormat::UInt32;
		xatlas::AddMeshError::Enum err = xatlas::AddMesh((xatlas::Atlas*)atlas, decl);
		if (err == xatlas::AddMeshError::IndexOutOfRange)
		{
			return 1;
		}
		else if (err == xatlas::AddMeshError::InvalidIndexCount)
		{
			return 2;
		}
		return 0;
	}

	_declspec(dllexport) int _cdecl xatlasAddUVMesh(void* atlas,
		int vertexCount, float* uv, int indexCount, unsigned int* indices32, bool allowRotate)
	{
		xatlas::UvMeshDecl decl;
		memset(&decl, 0, sizeof(decl));
		decl.vertexCount = vertexCount;
		decl.vertexUvData = uv;
		decl.vertexStride = sizeof(float) * 2;
		decl.indexCount = indexCount;
		decl.indexData = (void*)indices32;
		decl.indexFormat = xatlas::IndexFormat::UInt32;
		//decl.rotateCharts = allowRotate;
		//decl.singleChart = true;

		xatlas::AddMeshError::Enum err = xatlas::AddUvMesh((xatlas::Atlas*)atlas, decl);
		if (err == xatlas::AddMeshError::IndexOutOfRange)
		{
			return 1;
		}
		else if (err == xatlas::AddMeshError::InvalidIndexCount)
		{
			return 2;
		}
		return 0;
	}

	_declspec(dllexport) void _cdecl xatlasParametrizeExt(void* atlas, bool useInputMeshUVs)
	{
		xatlas::ChartOptions options;

		//options.useInputMeshUvs = useInputMeshUVs;

		xatlas::ComputeCharts((xatlas::Atlas*)atlas, options);
		xatlas::ParameterizeFunc((xatlas::Atlas*)atlas);
	}

	_declspec(dllexport) void _cdecl xatlasOnlyPackCharts(void* atlas)
	{
		xatlas::ParameterizeFunc((xatlas::Atlas*)atlas);
	}

	_declspec(dllexport) void _cdecl xatlasPackExt(void* atlas, int resolution, int texelSize, int padding, bool bruteForce, int maxChartSize, bool blockAlign)
	{
		xatlas::PackOptions options;
		options.bilinear = true;
		options.blockAlign = blockAlign;
		options.padding = padding;
		options.resolution = resolution;
		options.bruteForce = bruteForce;
		options.maxChartSize = maxChartSize;
		options.texelsPerUnit = texelSize;

		xatlas::PackCharts((xatlas::Atlas*)atlas, options);
	}

	_declspec(dllexport) int _cdecl xatlasGetAtlasCount(void* atlas)
	{
		xatlas::Atlas* a = (xatlas::Atlas*)atlas;
		return a->atlasCount;
	}

	_declspec(dllexport) int _cdecl xatlasGetAtlasIndex(void* atlas, int meshIndex, int chartIndex)
	{
		xatlas::Atlas* a = (xatlas::Atlas*)atlas;
		if (a->meshCount <= meshIndex) return 0;
		return a->meshes[meshIndex].chartArray[chartIndex].atlasIndex;
	}

	_declspec(dllexport) int _cdecl xatlasGetVertexCount(void* atlas, int meshIndex)
	{
		xatlas::Atlas* a = (xatlas::Atlas*)atlas;
		if (a->meshCount <= meshIndex) return 0;
		return a->meshes[meshIndex].vertexCount;
	}

	_declspec(dllexport) int _cdecl xatlasGetIndexCount(void* atlas, int meshIndex)
	{
		xatlas::Atlas* a = (xatlas::Atlas*)atlas;
		if (a->meshCount <= meshIndex) return 0;
		return a->meshes[meshIndex].indexCount;
	}

	struct MinMax2D
	{
		float minu, minv, maxu, maxv;
		float invLenU, invLenV;

		MinMax2D()
		{
			minu = FLT_MAX;
			minv = FLT_MAX;
			maxu = -FLT_MAX;
			maxv = -FLT_MAX;
		}
	};

	unsigned long nextPowerOfTwo(unsigned long v)
	{
		v--;
		v |= v >> 1;
		v |= v >> 2;
		v |= v >> 4;
		v |= v >> 8;
		v |= v >> 16;
		v++;
		return v;

	}
	_declspec(dllexport) void _cdecl xatlasNormalize(void* atlas, int* atlasSizes, bool preferDensity)
	{
		xatlas::Atlas* a = (xatlas::Atlas*)atlas;

		int atlasCount = xatlasGetAtlasCount(atlas);
		if (atlasCount == 0) return;

		MinMax2D* bounds = new MinMax2D[atlasCount];

		for (int meshIndex = 0; meshIndex < a->meshCount; meshIndex++)
		{
			int atlasIndex = 0;
			if (a->meshes[meshIndex].chartCount > 0)
			{
				atlasIndex = a->meshes[meshIndex].chartArray[0].atlasIndex;
			}
			xatlas::Vertex* verts = a->meshes[meshIndex].vertexArray;
			int numVerts = a->meshes[meshIndex].vertexCount;
			for (int i = 0; i < numVerts; i++)
			{
				float u = verts[i].uv[0];
				float v = verts[i].uv[1];
				if (u < bounds[atlasIndex].minu) bounds[atlasIndex].minu = u;
				if (u > bounds[atlasIndex].maxu) bounds[atlasIndex].maxu = u;
				if (v < bounds[atlasIndex].minv) bounds[atlasIndex].minv = v;
				if (v > bounds[atlasIndex].maxv) bounds[atlasIndex].maxv = v;
			}
		}
		if (atlasSizes != NULL)
		{
			for (int i = 0; i < atlasCount; i++)
			{
				float pwidth = bounds[i].maxu - bounds[i].minu;
				float pheight = bounds[i].maxv - bounds[i].minv;
				float psize = fmax(pwidth, pheight);
				unsigned long size = nextPowerOfTwo((unsigned long)psize);

				if (preferDensity) size = max(a->width, a->height);

				size = max(size, 16);
				size = min(size, max(a->width, a->height));
				atlasSizes[i] = (int)size;

				if (preferDensity)
				{
					bounds[i].maxu = size;
					bounds[i].maxv = size;
				}
			}
		}
		for (int i = 0; i < atlasCount; i++)
		{
			if (preferDensity)
			{
				bounds[i].invLenU = 1.0f / (bounds[i].maxu);
				bounds[i].invLenV = 1.0f / (bounds[i].maxv);
			}
			else
			{
				bounds[i].invLenU = 1.0f / (bounds[i].maxu - bounds[i].minu);
				bounds[i].invLenV = 1.0f / (bounds[i].maxv - bounds[i].minv);
			}
		}
		for (int meshIndex = 0; meshIndex < a->meshCount; meshIndex++)
		{
			int atlasIndex = 0;
			if (a->meshes[meshIndex].chartCount > 0)
			{
				atlasIndex = a->meshes[meshIndex].chartArray[0].atlasIndex;
			}
			xatlas::Vertex* verts = a->meshes[meshIndex].vertexArray;
			int numVerts = a->meshes[meshIndex].vertexCount;
			for (int i = 0; i < numVerts; i++)
			{
				verts[i].uv[0] = (verts[i].uv[0] - bounds[atlasIndex].minu) * bounds[atlasIndex].invLenU;
				verts[i].uv[1] = (verts[i].uv[1] - bounds[atlasIndex].minv) * bounds[atlasIndex].invLenV;
			}
		}

		delete[] bounds;
	}

	_declspec(dllexport) void _cdecl xatlasGetData(void* atlas, int meshIndex, float* outUV, int* outRef, int* outIndices)
	{
		xatlas::Atlas* a = (xatlas::Atlas*)atlas;
		if (a->meshCount <= meshIndex) return;

		xatlas::Vertex* verts = a->meshes[meshIndex].vertexArray;
		int numVerts = a->meshes[meshIndex].vertexCount;

		for (int i = 0; i < numVerts; i++)
		{
			outUV[i * 2] = verts[i].uv[0];
			outUV[i * 2 + 1] = verts[i].uv[1];

			outRef[i] = verts[i].xref;
		}

		uint32_t* indices = a->meshes[meshIndex].indexArray;
		int numIndices = a->meshes[meshIndex].indexCount;
		for (int i = 0; i < numIndices; i++)
		{
			outIndices[i] = indices[i];
		}
	}

	_declspec(dllexport) void _cdecl xatlasClear(void* atlas)
	{
		xatlas::Destroy((xatlas::Atlas*)atlas);
	}
}

