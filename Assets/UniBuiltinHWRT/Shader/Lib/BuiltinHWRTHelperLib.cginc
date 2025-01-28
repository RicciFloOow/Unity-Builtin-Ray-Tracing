#ifndef BUILTINHWRTHELPERLIB_INCLUDE
#define BUILTINHWRTHELPERLIB_INCLUDE

#include "UnityRaytracingMeshUtils.cginc"
#include "RTNoiseHelperLib.cginc"

RaytracingAccelerationStructure _SceneAccelStruct;

uint _RTCurrentFrame;

static const uint _MaxBounceCount = 8;//must <= given RTS's max_recursion_depth

struct RayPayload
{
    float3 color;
    uint bounceTimes;
    uint randomSeed;
};

//ref: HDRP RaytracingIntersection.hlsl
struct AttributeData
{
    // Barycentric value of the intersection
    //ref: https://learn.microsoft.com/en-us/windows/win32/direct3d12/intersection-attributes
    float2 barycentrics;
};

struct IntersectionVertex
{
    // Object space position of the vertex
    float3 positionOS;
    // Object space normal of the vertex
    float3 normalOS;
    // Object space tangent of the vertex
    float4 tangentOS;
    // UV coordinates
    //如果我没记错的话unity fbx实际使用的uv是不支持float4的，因此这里只用float2即可
    float2 texCoord0;
    float2 texCoord1;
    float2 texCoord2;
    float2 texCoord3;
    float4 color;
};

// Macro that interpolate any attribute using barycentric coordinates
#define INTERPOLATE_RAYTRACING_ATTRIBUTE(A0, A1, A2, BARYCENTRIC_COORDINATES) (A0 * BARYCENTRIC_COORDINATES.x + A1 * BARYCENTRIC_COORDINATES.y + A2 * BARYCENTRIC_COORDINATES.z)

// Fetch the intersetion vertex data for the target vertex
void FetchIntersectionVertex(uint vertexIndex, out IntersectionVertex outVertex)
{
    outVertex.positionOS = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributePosition);
    outVertex.normalOS = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeNormal);
    outVertex.tangentOS = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeTangent);
    outVertex.texCoord0 = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
    outVertex.texCoord1 = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord1);
    outVertex.texCoord2 = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord2);
    outVertex.texCoord3 = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord3);
    outVertex.color = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeColor);
}

void GetCurrentIntersectionVertex(AttributeData attributeData, out IntersectionVertex outVertex)
{
    // Fetch the indices of the currentr triangle
    uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());

    // Fetch the 3 vertices
    IntersectionVertex v0, v1, v2;
    FetchIntersectionVertex(triangleIndices.x, v0);
    FetchIntersectionVertex(triangleIndices.y, v1);
    FetchIntersectionVertex(triangleIndices.z, v2);

    // Compute the full barycentric coordinates
    float3 barycentricCoordinates = float3(1.0 - attributeData.barycentrics.x - attributeData.barycentrics.y, attributeData.barycentrics.x, attributeData.barycentrics.y);

    // Interpolate all the data
    outVertex.positionOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.positionOS, v1.positionOS, v2.positionOS, barycentricCoordinates);
    outVertex.normalOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.normalOS, v1.normalOS, v2.normalOS, barycentricCoordinates);
    outVertex.tangentOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.tangentOS, v1.tangentOS, v2.tangentOS, barycentricCoordinates);
    outVertex.texCoord0 = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord0, v1.texCoord0, v2.texCoord0, barycentricCoordinates);
    outVertex.texCoord1 = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord1, v1.texCoord1, v2.texCoord1, barycentricCoordinates);
    outVertex.texCoord2 = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord2, v1.texCoord2, v2.texCoord2, barycentricCoordinates);
    outVertex.texCoord3 = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord3, v1.texCoord3, v2.texCoord3, barycentricCoordinates);
    outVertex.color = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.color, v1.color, v2.color, barycentricCoordinates);
}
#endif