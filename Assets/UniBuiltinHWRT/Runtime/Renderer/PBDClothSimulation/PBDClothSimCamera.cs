//TODO:改个small steps的
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UniBuiltinHWRT
{
    public partial class PBDClothSimCamera : MonoBehaviour
    {
        #region ----Rendering Settings----
        [Range(0.000001f, 0.02f)]
        public float SimulationDeltaTime = 0.001f;
        [Range(2, 64)]
        public int SolverIteratorCount = 4;
        #endregion

        #region ----PBD Cloth Simulate Pass----
        private const CameraEvent k_PBDClothSimulatePassCamEvent = CameraEvent.AfterForwardOpaque;
        private CommandBuffer m_PBDSimulationPassBuffer;
        private bool m_IsVerticesMassNeedInit;
        private bool m_IsFixedPointPosNeedInit;//这个理论上只需要执行一次(至少是本demo中)，但是如果允许布料被破坏的话，那么顶点变化时也是需要更新的

        private float GetIterationStiffness(float stiffness, int iterationCount)
        {
            return 1 - Mathf.Pow(1 - stiffness, 1.0f / iterationCount);
        }

        private void ExecuteInitDynamicVerticesKernel(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("InitDynamicVerticesKernel");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeIntParam(cs, k_ShaderProperty_Uint_MeshVerticesCount, TargetPBDCloth.MeshVerticesCount);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_VerticesFixedWeight, TargetPBDCloth.Mesh_VerticesFixedWeight);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_BonesPerVertex, TargetPBDCloth.Mesh_BonesPerVertex);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_AllBoneWeights, TargetPBDCloth.Mesh_AllBoneWeights);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_BonesLocalToWorldBuffer, TargetPBDCloth.Mesh_BonesLocalToWorldBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Mesh_Dynamic_VerticesBuffer, TargetPBDCloth.Mesh_Dynamic_VerticesBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteGetVerticesMassKernel(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("GetVerticesMassKernel");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeFloatParam(cs, k_ShaderProperty_Float_MeshDensity, TargetPBDCloth.ClothDensity);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_TrianglesPerVertex, TargetPBDCloth.Mesh_TrianglesPerVertex);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_AllVertexTriangles, TargetPBDCloth.Mesh_AllVertexTriangles);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_IndicesBuffer, TargetPBDCloth.Mesh_IndicesBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_Dynamic_VerticesBuffer, TargetPBDCloth.Mesh_Dynamic_VerticesBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_MassBuffer, TargetPBDCloth.Simu_MassBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteInitFixedPointPosKernel(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("InitFixedPointPosKernel");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeIntParam(cs, k_ShaderProperty_Uint_MeshVerticesCount, TargetPBDCloth.MeshVerticesCount);//这里还是得传一下
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Mesh_Static_VerticesBuffer, TargetPBDCloth.Mesh_Static_VerticesBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteGetAnimatedFixedPointPosKernel(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("GetAnimatedFixedPointPosKernel");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeIntParam(cs, k_ShaderProperty_Uint_MeshVerticesCount, TargetPBDCloth.MeshVerticesCount);//这里还是得传一下
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_VerticesFixedWeight, TargetPBDCloth.Mesh_VerticesFixedWeight);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_BonesPerVertex, TargetPBDCloth.Mesh_BonesPerVertex);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_AllBoneWeights, TargetPBDCloth.Mesh_AllBoneWeights);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_BonesLocalToWorldBuffer, TargetPBDCloth.Mesh_BonesLocalToWorldBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Mesh_Static_VerticesBuffer, TargetPBDCloth.Mesh_Static_VerticesBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteInitTempPosVelBuffersKernel(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("InitTempPosVelBuffersKernel");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_TempVelocityBuffer, TargetPBDCloth.Simu_TempVelocityBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_TempPositionBuffer, TargetPBDCloth.Simu_TempPositionBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteApplyExternalForceKernel(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("ApplyExternalForceKernel");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeFloatParam(cs, k_ShaderProperty_Float_SimuDeltaTime, SimulationDeltaTime);
            cmd.SetComputeVectorParam(cs, k_ShaderProperty_Vec_WindForce, Vector3.zero);//TODO:
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_MassBuffer, TargetPBDCloth.Simu_MassBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_VelocityBuffer, TargetPBDCloth.Simu_VelocityBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_Dynamic_VerticesBuffer, TargetPBDCloth.Mesh_Dynamic_VerticesBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_TempVelocityBuffer, TargetPBDCloth.Simu_TempVelocityBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_TempPositionBuffer, TargetPBDCloth.Simu_TempPositionBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecutePBDVerticesCollisionDetectionKernel(ref CommandBuffer cmd, RayTracingShader rts)
        {
            cmd.SetRayTracingShaderPass(rts, "PBDClothCollision");
            cmd.SetRayTracingFloatParam(rts, k_ShaderProperty_Float_ClothThickness, TargetPBDCloth.ClothThickness);
            cmd.SetRayTracingBufferParam(rts, k_ShaderProperty_Buffer_Mesh_Dynamic_VerticesBuffer, TargetPBDCloth.Mesh_Dynamic_VerticesBuffer);
            cmd.SetRayTracingBufferParam(rts, k_ShaderProperty_Buffer_Mesh_Dynamic_NormalsBuffer, TargetPBDCloth.Mesh_Dynamic_NormalsBuffer);
            cmd.SetRayTracingBufferParam(rts, k_ShaderProperty_Buffer_Simu_TempPositionBuffer, TargetPBDCloth.Simu_TempPositionBuffer);
            cmd.SetRayTracingBufferParam(rts, k_ShaderProperty_Buffer_RW_Simu_CollisionResultBuffer, TargetPBDCloth.Simu_CollisionResultBuffer);
            cmd.SetRayTracingAccelerationStructure(rts, k_ShaderProperty_SceneAccelStruct, m_RTAccStruct);
            cmd.DispatchRays(rts, "PBDVerticesCollisionDetection", (uint)TargetPBDCloth.MeshVerticesCount, 1, 1);
        }

        private void ExecuteInitConstraintDeltaPosBuffersKernel(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("InitConstraintDeltaPosBuffersKernel");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_DisConstraintDeltaPosBuffer, TargetPBDCloth.Simu_DisConstraintDeltaPosBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_BendConstraintDeltaPosBuffer, TargetPBDCloth.Simu_BendConstraintDeltaPosBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteApplyDistanceConstraint(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("ApplyDistanceConstraint");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeIntParam(cs, k_ShaderProperty_Uint_MeshVerticesCount, TargetPBDCloth.MeshVerticesCount);
            cmd.SetComputeFloatParam(cs, k_ShaderProperty_Float_DistanceConstraintStiffness, GetIterationStiffness(TargetPBDCloth.DistanceConstraintStiffness, SolverIteratorCount));
            cmd.SetComputeFloatParam(cs, k_ShaderProperty_Float_SolverIteratorCount, SolverIteratorCount);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_TempPositionBuffer, TargetPBDCloth.Simu_TempPositionBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_MassBuffer, TargetPBDCloth.Simu_MassBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_EdgesPerVertex, TargetPBDCloth.Mesh_EdgesPerVertex);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_AllVertexEdges, TargetPBDCloth.Mesh_AllVertexEdges);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_DisConstraintDeltaPosBuffer, TargetPBDCloth.Simu_DisConstraintDeltaPosBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteApplyBendingConstraint(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("ApplyBendingConstraint");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeFloatParam(cs, k_ShaderProperty_Float_BendingConstraintStiffness, GetIterationStiffness(TargetPBDCloth.BendingConstraintStiffness, SolverIteratorCount));
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_TempPositionBuffer, TargetPBDCloth.Simu_TempPositionBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_MassBuffer, TargetPBDCloth.Simu_MassBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_SharedEdgesPerVertex, TargetPBDCloth.Mesh_SharedEdgesPerVertex);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_AllVertexSharedEdges, TargetPBDCloth.Mesh_AllVertexSharedEdges);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_BendConstraintDeltaPosBuffer, TargetPBDCloth.Simu_BendConstraintDeltaPosBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteCalculateAppliedConstraintsTempPosition(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("CalculateAppliedConstraintsTempPosition");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_DisConstraintDeltaPosBuffer, TargetPBDCloth.Simu_DisConstraintDeltaPosBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_BendConstraintDeltaPosBuffer, TargetPBDCloth.Simu_BendConstraintDeltaPosBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_TempPositionBuffer, TargetPBDCloth.Simu_TempPositionBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteApplyConstraints(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("ApplyConstraints");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_Static_VerticesBuffer, TargetPBDCloth.Mesh_Static_VerticesBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_TempPositionBuffer, TargetPBDCloth.Simu_TempPositionBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_TempVelocityBuffer, TargetPBDCloth.Simu_TempVelocityBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Simu_CollisionResultBuffer, TargetPBDCloth.Simu_CollisionResultBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Mesh_Dynamic_VerticesBuffer, TargetPBDCloth.Mesh_Dynamic_VerticesBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Simu_VelocityBuffer, TargetPBDCloth.Simu_VelocityBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ExecuteCalculateVerticesNormal(ref CommandBuffer cmd, ComputeShader cs)
        {
            int kernelIndex = cs.FindKernel("CalculateVerticesNormal");
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_TrianglesPerVertex, TargetPBDCloth.Mesh_TrianglesPerVertex);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_AllVertexTriangles, TargetPBDCloth.Mesh_AllVertexTriangles);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_IndicesBuffer, TargetPBDCloth.Mesh_IndicesBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_Mesh_Dynamic_VerticesBuffer, TargetPBDCloth.Mesh_Dynamic_VerticesBuffer);
            cmd.SetComputeBufferParam(cs, kernelIndex, k_ShaderProperty_Buffer_RW_Mesh_Dynamic_NormalsBuffer, TargetPBDCloth.Mesh_Dynamic_NormalsBuffer);
            cmd.DispatchCompute(cs, kernelIndex, Mathf.CeilToInt(TargetPBDCloth.MeshVerticesCount / (float)x), 1, 1);
        }

        private void ReleasePBDSimulationPassBuffer()
        {
            if (m_renderCam == null)
            {
                return;
            }
            if (m_PBDSimulationPassBuffer != null)
            {
                m_renderCam.RemoveCommandBuffer(k_PBDClothSimulatePassCamEvent, m_PBDSimulationPassBuffer);
                m_PBDSimulationPassBuffer.Release();
                m_PBDSimulationPassBuffer = null;
            }
        }

        private void SetupPBDSimulationPassBuffer()
        {
            ReleasePBDSimulationPassBuffer();
            if (m_renderCam == null)
            {
                return;
            }
            if (TargetPBDCloth == null)
            {
                return;
            }
            m_PBDSimulationPassBuffer = new CommandBuffer()
            {
                name = "PBD Simulation Pass"
            };
            //
            if (m_IsVerticesMassNeedInit)
            {
                m_IsVerticesMassNeedInit = false;
                ExecuteInitDynamicVerticesKernel(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
                ExecuteGetVerticesMassKernel(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
            }
            if (m_IsFixedPointPosNeedInit)
            {
                m_IsFixedPointPosNeedInit = false;
                ExecuteInitFixedPointPosKernel(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
            }
            //
            ExecuteGetAnimatedFixedPointPosKernel(ref m_PBDSimulationPassBuffer, PBDSimulationCS);//这个其实可以单独低频更新(与动画帧率一致)
            ExecuteInitTempPosVelBuffersKernel(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
            ExecuteApplyExternalForceKernel(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
            ExecutePBDVerticesCollisionDetectionKernel(ref m_PBDSimulationPassBuffer, PBDCollisionRTS);
            ExecuteInitConstraintDeltaPosBuffersKernel(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
            for (int i = 0; i < SolverIteratorCount; i++)
            {

                ExecuteApplyDistanceConstraint(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
                ExecuteApplyBendingConstraint(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
                ExecuteCalculateAppliedConstraintsTempPosition(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
                //
            }
            ExecuteApplyConstraints(ref m_PBDSimulationPassBuffer, PBDSimulationCS);//需要注意，我们这里更改了碰撞检测的顺序，并且用的碰撞检测模型是极其简陋的(仅为了验证RT布料模拟管线的可行性)
            ExecuteCalculateVerticesNormal(ref m_PBDSimulationPassBuffer, PBDSimulationCS);
            //绘制
            if (TargetPBDCloth.Material != null)
            {
                MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();
                matPropertyBlock.SetMatrix(k_ShaderProperty_Matrix_ClothWorld2ObjectMat, TargetPBDCloth.transform.worldToLocalMatrix.transpose);
                matPropertyBlock.SetBuffer(k_ShaderProperty_Buffer_Mesh_IndicesBuffer, TargetPBDCloth.Mesh_IndicesBuffer);
                matPropertyBlock.SetBuffer(k_ShaderProperty_Buffer_Mesh_UVBuffer, TargetPBDCloth.Mesh_UVBuffer);
                matPropertyBlock.SetBuffer(k_ShaderProperty_Buffer_Mesh_Dynamic_VerticesBuffer, TargetPBDCloth.Mesh_Dynamic_VerticesBuffer);
                matPropertyBlock.SetBuffer(k_ShaderProperty_Buffer_Mesh_Dynamic_NormalsBuffer, TargetPBDCloth.Mesh_Dynamic_NormalsBuffer);
                m_PBDSimulationPassBuffer.DrawProcedural(Matrix4x4.identity, TargetPBDCloth.Material, 0, MeshTopology.Triangles, TargetPBDCloth.MeshIndicesCount, 1, matPropertyBlock);
            }
            //
            m_renderCam.AddCommandBuffer(k_PBDClothSimulatePassCamEvent, m_PBDSimulationPassBuffer);
        }
        #endregion

        #region ----Unity----
        private void OnEnable()
        {
            SetupRenderCam();
        }

        private void Start()
        {
            SetupRayTracingAccelStruct();
            m_IsVerticesMassNeedInit = true;
            m_IsFixedPointPosNeedInit = true;
        }

        private void Update()
        {
            UpdateRTAS();
        }

        private void OnPreRender()
        {
            SetupPBDSimulationPassBuffer();
        }

        private void OnDisable()
        {
            ReleasePBDSimulationPassBuffer();
        }

        private void OnDestroy()
        {
            ReleaseRayTracingAccelStruct();
        }
        #endregion
    }
}