#include "xatlas.h"
#include <algorithm>

#if defined(_WIN32) || defined(_WIN64)
#define EXPORT __declspec(dllexport)
#define CALLCONV __cdecl
#else
#define EXPORT __attribute__((visibility("default")))
#define CALLCONV
#endif

extern "C"
{
    struct Mesh
    {
        float *vertices;
        float *uvs;
        uint32_t *indices;

        uint32_t vertexCount;
        uint32_t indexCount;
        uint32_t submeshIndex;
    };

    EXPORT float CALLCONV PackLightmap(Mesh *meshes, uint32_t meshLength, bool blockAlign, uint32_t padding, uint32_t resolution, bool bruteForce, uint32_t maxChartSize, float texelSize)
    {

        xatlas::Atlas *atlas = xatlas::Create();

        for (uint32_t i = 0; i < meshLength; i++)
        {
            Mesh *mesh = &meshes[i];

            xatlas::UvMeshDecl decl;
            decl.vertexCount = mesh->vertexCount;
            decl.vertexUvData = mesh->uvs;
            decl.vertexStride = sizeof(float) * 2;
            decl.indexCount = mesh->indexCount;
            decl.indexData = mesh->indices;
            decl.indexFormat = xatlas::IndexFormat::UInt32;

            xatlas::AddMeshError err = xatlas::AddUvMesh(atlas, decl);

            if (err == xatlas::AddMeshError::IndexOutOfRange)
            {
                return 1;
            }
            else if (err == xatlas::AddMeshError::InvalidIndexCount)
            {
                return 2;
            }
        }

        xatlas::PackOptions packOptions;
        packOptions.bilinear = true;
        packOptions.blockAlign = blockAlign;
        packOptions.padding = padding;
        packOptions.resolution = resolution;
        packOptions.bruteForce = bruteForce;
        packOptions.maxChartSize = maxChartSize;
        packOptions.texelsPerUnit = texelSize;

        xatlas::ChartOptions chartOptions;
        chartOptions.useInputMeshUvs = true;

        xatlas::ComputeCharts(atlas, chartOptions);

        xatlas::PackCharts(atlas, packOptions);

        float max[2] = {1};

        for (uint32_t i = 0; i < meshLength; i++)
        {
            Mesh *mesh = &meshes[i];

            xatlas::Vertex *verts = atlas->meshes[i].vertexArray;
            uint32_t vertexCount = atlas->meshes[i].vertexCount;

            float uvX = 0;
            float uvY = 0;

            for (uint32_t j = 0; j < vertexCount; j++)
            {
                uint32_t xref = verts[j].xref;

                uvX = verts[j].uv[0];
                uvY = verts[j].uv[1];

                max[0] = std::max(max[0], uvX);
                max[1] = std::max(max[1], uvY);
            }
        }

        float scale[2] = {1};

        scale[0] = 1.0 / max[0];
        scale[1] = 1.0 / max[1];

        for (uint32_t i = 0; i < meshLength; i++)
        {
            Mesh *mesh = &meshes[i];

            xatlas::Vertex *verts = atlas->meshes[i].vertexArray;
            uint32_t vertexCount = atlas->meshes[i].vertexCount;

            float uvX = 0;
            float uvY = 0;

            for (uint32_t j = 0; j < vertexCount; j++)
            {
                uint32_t xref = verts[j].xref;

                uvX = verts[j].uv[0] * scale[0];
                uvY = verts[j].uv[1] * scale[1];

                mesh->uvs[xref * 2] = uvX;
                mesh->uvs[xref * 2 + 1] = uvY;
            }
        }

        xatlas::Destroy(atlas);

        return 0;
    }
}