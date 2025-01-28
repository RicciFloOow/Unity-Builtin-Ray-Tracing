#ifndef PBDCLOTHINPUTS_INCLUDE
#define PBDCLOTHINPUTS_INCLUDE

struct BoneWeight1
{
	float Weight;
	int BoneIndex;
};

struct PBDEdge
{
	int Index;//the other vertex's index
	float BaseLength;
};

struct PBDSharedEdge
{
	int Index;
	int LTIndex;
	int RBIndex;
	float BaseAngle;
};

struct PBDGPUFixedWeight
{
	float3 Vertex;
	float Weight;
};

struct RayPayload
{
	float HitCurrent;
	float3 Normal;
	bool IsHit;
};

struct ClothCollisionResult
{
	float4 HitPoint;//xyz: hit pos, w: is hit (1 hit, 0 not hit)
	float3 HitNormal;
};

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
    float2 texCoord0;
    float2 texCoord1;
    float2 texCoord2;
    float2 texCoord3;
    float4 color;
};

uint _MeshVerticesCount;

float _SolverIteratorCount;
float _MeshDensity;//note that we use a constant density for our demo cloth mesh
float _SimuDeltaTime;
float _DistanceConstraintStiffness;//we can also divide this into stretch and compress
float _BendingConstraintStiffness;
float _ClothThickness;

float3 _WindForce;

StructuredBuffer<float4> _Mesh_Static_VerticesBuffer;//xyz:pos (applied by bones), w:fixed weight
StructuredBuffer<float3> _Mesh_Dynamic_VerticesBuffer;
StructuredBuffer<float3> _Mesh_Dynamic_NormalsBuffer;
StructuredBuffer<float2> _Mesh_UVBuffer;
StructuredBuffer<int> _Mesh_IndicesBuffer;
StructuredBuffer<int> _Mesh_BonesPerVertex;
StructuredBuffer<BoneWeight1> _Mesh_AllBoneWeights;
StructuredBuffer<int> _Mesh_EdgesPerVertex;
StructuredBuffer<PBDEdge> _Mesh_AllVertexEdges;
StructuredBuffer<int> _Mesh_SharedEdgesPerVertex;
StructuredBuffer<PBDSharedEdge> _Mesh_AllVertexSharedEdges;
StructuredBuffer<int> _Mesh_TrianglesPerVertex;
StructuredBuffer<int> _Mesh_AllVertexTriangles;
StructuredBuffer<PBDGPUFixedWeight> _Mesh_VerticesFixedWeight;
StructuredBuffer<float4x4> _Mesh_BonesLocalToWorldBuffer;
StructuredBuffer<float3> _Simu_TempPositionBuffer;
StructuredBuffer<float3> _Simu_TempVelocityBuffer;
StructuredBuffer<float3> _Simu_DisConstraintDeltaPosBuffer;
StructuredBuffer<float3> _Simu_BendConstraintDeltaPosBuffer;
StructuredBuffer<float3> _Simu_VelocityBuffer;
StructuredBuffer<float> _Simu_MassBuffer;
StructuredBuffer<ClothCollisionResult> _Simu_CollisionResultBuffer;

RWStructuredBuffer<float4> RW_Mesh_Static_VerticesBuffer;//here static means the vertices are fully controlled by bones
RWStructuredBuffer<float3> RW_Mesh_Dynamic_VerticesBuffer;
RWStructuredBuffer<float3> RW_Mesh_Dynamic_NormalsBuffer;
RWStructuredBuffer<float3> RW_Simu_TempPositionBuffer;
RWStructuredBuffer<float3> RW_Simu_TempVelocityBuffer;
RWStructuredBuffer<float3> RW_Simu_DisConstraintDeltaPosBuffer;
RWStructuredBuffer<float3> RW_Simu_BendConstraintDeltaPosBuffer;
RWStructuredBuffer<float3> RW_Simu_VelocityBuffer;
RWStructuredBuffer<float> RW_Simu_MassBuffer;
RWStructuredBuffer<ClothCollisionResult> RW_Simu_CollisionResultBuffer;

//for constant stiffness, do this in cpu and pass it to gpu
inline float GetIterationStiffness(float stiffness, uint iterationCount)
{
	return 1 - pow(1 - stiffness, 1.0 / (float)iterationCount);
}

#endif