using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniBuiltinHWRT
{
    public partial class PBDClothSimCamera : MonoBehaviour
    {
        #region ----Uint/Int/Float----
        private readonly static int k_ShaderProperty_Uint_MeshVerticesCount = Shader.PropertyToID("_MeshVerticesCount");
        private readonly static int k_ShaderProperty_Float_SolverIteratorCount = Shader.PropertyToID("_SolverIteratorCount");
        private readonly static int k_ShaderProperty_Float_MeshDensity = Shader.PropertyToID("_MeshDensity");
        private readonly static int k_ShaderProperty_Float_SimuDeltaTime = Shader.PropertyToID("_SimuDeltaTime");
        private readonly static int k_ShaderProperty_Float_DistanceConstraintStiffness = Shader.PropertyToID("_DistanceConstraintStiffness");
        private readonly static int k_ShaderProperty_Float_BendingConstraintStiffness = Shader.PropertyToID("_BendingConstraintStiffness");
        private readonly static int k_ShaderProperty_Float_ClothThickness = Shader.PropertyToID("_ClothThickness");
        #endregion

        #region ----Vector----
        private readonly static int k_ShaderProperty_Vec_WindForce = Shader.PropertyToID("_WindForce");
        #endregion

        #region ----Matrix----
        private readonly static int k_ShaderProperty_Matrix_ClothWorld2ObjectMat = Shader.PropertyToID("_ClothWorld2ObjectMat");
        #endregion

        #region ----Buffer----
        private readonly static int k_ShaderProperty_Buffer_Mesh_Static_VerticesBuffer = Shader.PropertyToID("_Mesh_Static_VerticesBuffer");
        private readonly static int k_ShaderProperty_Buffer_Mesh_Dynamic_VerticesBuffer = Shader.PropertyToID("_Mesh_Dynamic_VerticesBuffer");
        private readonly static int k_ShaderProperty_Buffer_Mesh_Dynamic_NormalsBuffer = Shader.PropertyToID("_Mesh_Dynamic_NormalsBuffer");
        private readonly static int k_ShaderProperty_Buffer_Mesh_UVBuffer = Shader.PropertyToID("_Mesh_UVBuffer");
        private readonly static int k_ShaderProperty_Buffer_Mesh_IndicesBuffer = Shader.PropertyToID("_Mesh_IndicesBuffer");
        private readonly static int k_ShaderProperty_Buffer_Mesh_BonesPerVertex = Shader.PropertyToID("_Mesh_BonesPerVertex");
        private readonly static int k_ShaderProperty_Buffer_Mesh_AllBoneWeights = Shader.PropertyToID("_Mesh_AllBoneWeights");
        private readonly static int k_ShaderProperty_Buffer_Mesh_EdgesPerVertex = Shader.PropertyToID("_Mesh_EdgesPerVertex");
        private readonly static int k_ShaderProperty_Buffer_Mesh_AllVertexEdges = Shader.PropertyToID("_Mesh_AllVertexEdges");
        private readonly static int k_ShaderProperty_Buffer_Mesh_SharedEdgesPerVertex = Shader.PropertyToID("_Mesh_SharedEdgesPerVertex");
        private readonly static int k_ShaderProperty_Buffer_Mesh_AllVertexSharedEdges = Shader.PropertyToID("_Mesh_AllVertexSharedEdges");
        private readonly static int k_ShaderProperty_Buffer_Mesh_TrianglesPerVertex = Shader.PropertyToID("_Mesh_TrianglesPerVertex");
        private readonly static int k_ShaderProperty_Buffer_Mesh_AllVertexTriangles = Shader.PropertyToID("_Mesh_AllVertexTriangles");
        private readonly static int k_ShaderProperty_Buffer_Mesh_VerticesFixedWeight = Shader.PropertyToID("_Mesh_VerticesFixedWeight");
        private readonly static int k_ShaderProperty_Buffer_Mesh_BonesLocalToWorldBuffer = Shader.PropertyToID("_Mesh_BonesLocalToWorldBuffer");
        private readonly static int k_ShaderProperty_Buffer_Simu_TempPositionBuffer = Shader.PropertyToID("_Simu_TempPositionBuffer");
        private readonly static int k_ShaderProperty_Buffer_Simu_TempVelocityBuffer = Shader.PropertyToID("_Simu_TempVelocityBuffer");
        private readonly static int k_ShaderProperty_Buffer_Simu_DisConstraintDeltaPosBuffer = Shader.PropertyToID("_Simu_DisConstraintDeltaPosBuffer");
        private readonly static int k_ShaderProperty_Buffer_Simu_BendConstraintDeltaPosBuffer = Shader.PropertyToID("_Simu_BendConstraintDeltaPosBuffer");
        private readonly static int k_ShaderProperty_Buffer_Simu_VelocityBuffer = Shader.PropertyToID("_Simu_VelocityBuffer");
        private readonly static int k_ShaderProperty_Buffer_Simu_MassBuffer = Shader.PropertyToID("_Simu_MassBuffer");
        private readonly static int k_ShaderProperty_Buffer_Simu_CollisionResultBuffer = Shader.PropertyToID("_Simu_CollisionResultBuffer");

        private readonly static int k_ShaderProperty_Buffer_RW_Mesh_Static_VerticesBuffer = Shader.PropertyToID("RW_Mesh_Static_VerticesBuffer");
        private readonly static int k_ShaderProperty_Buffer_RW_Mesh_Dynamic_VerticesBuffer = Shader.PropertyToID("RW_Mesh_Dynamic_VerticesBuffer");
        private readonly static int k_ShaderProperty_Buffer_RW_Mesh_Dynamic_NormalsBuffer = Shader.PropertyToID("RW_Mesh_Dynamic_NormalsBuffer");
        private readonly static int k_ShaderProperty_Buffer_RW_Simu_TempPositionBuffer = Shader.PropertyToID("RW_Simu_TempPositionBuffer");
        private readonly static int k_ShaderProperty_Buffer_RW_Simu_TempVelocityBuffer = Shader.PropertyToID("RW_Simu_TempVelocityBuffer");
        private readonly static int k_ShaderProperty_Buffer_RW_Simu_DisConstraintDeltaPosBuffer = Shader.PropertyToID("RW_Simu_DisConstraintDeltaPosBuffer");
        private readonly static int k_ShaderProperty_Buffer_RW_Simu_BendConstraintDeltaPosBuffer = Shader.PropertyToID("RW_Simu_BendConstraintDeltaPosBuffer");
        private readonly static int k_ShaderProperty_Buffer_RW_Simu_VelocityBuffer = Shader.PropertyToID("RW_Simu_VelocityBuffer");
        private readonly static int k_ShaderProperty_Buffer_RW_Simu_MassBuffer = Shader.PropertyToID("RW_Simu_MassBuffer");
        private readonly static int k_ShaderProperty_Buffer_RW_Simu_CollisionResultBuffer = Shader.PropertyToID("RW_Simu_CollisionResultBuffer");
        #endregion

        #region ----RT Acc Structure----
        private readonly static int k_ShaderProperty_SceneAccelStruct = Shader.PropertyToID("_SceneAccelStruct");
        #endregion
    }
}