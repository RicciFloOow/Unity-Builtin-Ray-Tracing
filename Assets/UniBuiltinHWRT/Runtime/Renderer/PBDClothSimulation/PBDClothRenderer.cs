//需要注意的是，我们将默认网格在渲染时不会有额外的缩放，如果有，也需要是等比例的，否则需要重新计算边的BaseLength(等比例的只需要乘个系数即可)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UniBuiltinHWRT
{
    public class PBDClothRenderer : MonoBehaviour
    {
        #region ----Rendering Datas----
        public PBDClothMeshData Mesh;
        public Material Material;
        public SkinnedMeshRenderer RelatedSkinnedMesh;
        [Range(0.00001f, 10.0f)]
        public float ClothDensity = 0.4f;
        [Range(0f, 1f)]
        public float DistanceConstraintStiffness = 0.25f;
        [Range(0f, 1f)]
        public float BendingConstraintStiffness = 0.2f;
        [Range(0.00001f, 0.1f)]
        public float ClothThickness = 0.001f;
        #endregion

        public int MeshIndicesCount { get; private set; }
        public int MeshVerticesCount { get; private set; }

        #region ----Skinned Mesh Bones----
        private Transform[] m_bonesTransforms;

        private void SetupBonesData()
        {
            RelatedSkinnedMesh.enabled = false;
            m_bonesTransforms = RelatedSkinnedMesh.bones;
        }

        private void UpdateAnimatedBonesData()
        {
            Matrix4x4[] bonesLocalToWorldData = new Matrix4x4[m_bonesTransforms.Length];
            //Matrix4x4 w2lMatrix = m_bonesTransforms[0].parent.worldToLocalMatrix;
            for (int i = 0; i < m_bonesTransforms.Length; i++)
            {
                Matrix4x4 bindP = Mesh.BindPoses[i];
                //Matrix4x4 l2wMat = (w2lMatrix * m_bonesTransforms[i].localToWorldMatrix * bindP);//如果要转到local的，那么需要乘个world2local
                Matrix4x4 l2wMat = (m_bonesTransforms[i].localToWorldMatrix * bindP);
                bonesLocalToWorldData[i] = l2wMat;
            }
            Mesh_BonesLocalToWorldBuffer.SetData(bonesLocalToWorldData);
        }
        #endregion

        #region ----Simulation Buffers----
        public ComputeBuffer Mesh_Static_VerticesBuffer;//存Fixed Points的原始顶点的数据
        public ComputeBuffer Mesh_Dynamic_VerticesBuffer;
        public ComputeBuffer Mesh_Dynamic_NormalsBuffer;//由m_Mesh_Dynamic_VerticesBuffer重新生成的，利用m_Mesh_TrianglesPerVertex求和再平均(也可以(基于面积)加权，不过加权出来的一般有点问题)
        public ComputeBuffer Mesh_UVBuffer;
        public ComputeBuffer Mesh_IndicesBuffer;
        //
        public ComputeBuffer Mesh_BonesPerVertex;
        public ComputeBuffer Mesh_AllBoneWeights;
        public ComputeBuffer Mesh_EdgesPerVertex;
        public ComputeBuffer Mesh_AllVertexEdges;
        public ComputeBuffer Mesh_SharedEdgesPerVertex;
        public ComputeBuffer Mesh_AllVertexSharedEdges;
        public ComputeBuffer Mesh_TrianglesPerVertex;
        public ComputeBuffer Mesh_AllVertexTriangles;
        public ComputeBuffer Mesh_VerticesFixedWeight;
        public ComputeBuffer Mesh_BonesLocalToWorldBuffer;
        //
        public ComputeBuffer Simu_TempPositionBuffer;
        public ComputeBuffer Simu_TempVelocityBuffer;
        public ComputeBuffer Simu_DisConstraintDeltaPosBuffer;
        public ComputeBuffer Simu_BendConstraintDeltaPosBuffer;
        public ComputeBuffer Simu_VelocityBuffer;
        public ComputeBuffer Simu_MassBuffer;
        public ComputeBuffer Simu_CollisionResultBuffer;

        private void SetupSimulationBuffers()
        {
            if (Mesh == null)
            {
                Debug.LogWarning("没有网格数据!");
                return;
            }
            //
            int verticesCount = Mesh.Vertices.Length;
            //
            MeshVerticesCount = verticesCount;
            MeshIndicesCount = Mesh.Indices.Length;
            //
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_Static_VerticesBuffer, verticesCount, Marshal.SizeOf(typeof(Vector4)));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_Dynamic_VerticesBuffer, verticesCount, Marshal.SizeOf(typeof(Vector3)));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_Dynamic_NormalsBuffer, verticesCount, Marshal.SizeOf(typeof(Vector3)));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_UVBuffer, verticesCount, Marshal.SizeOf(typeof(Vector2)));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_IndicesBuffer, Mesh.Indices.Length, sizeof(int));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_BonesPerVertex, Mesh.BonesPerVertex.Length, sizeof(int));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_AllBoneWeights, Mesh.AllBoneWeights.Length, Marshal.SizeOf(typeof(BoneWeight1)));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_EdgesPerVertex, Mesh.EdgesPerVertex.Length, sizeof(int));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_AllVertexEdges, Mesh.AllVertexEdges.Length, Marshal.SizeOf(typeof(PBDEdge)));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_SharedEdgesPerVertex, Mesh.SharedEdgesPerVertex.Length, sizeof(int));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_AllVertexSharedEdges, Mesh.AllVertexSharedEdges.Length, Marshal.SizeOf(typeof(PBDSharedEdge)));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_TrianglesPerVertex, Mesh.TrianglesPerVertex.Length, sizeof(int));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_AllVertexTriangles, Mesh.AllVertexTriangles.Length, sizeof(int));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_VerticesFixedWeight, verticesCount, Marshal.SizeOf(typeof(PBDGPUFixedWeight)));
            GraphicsUtility.AllocateComputeBuffer(ref Mesh_BonesLocalToWorldBuffer, Mesh.BindPoses.Length, Marshal.SizeOf(typeof(Matrix4x4)));
            GraphicsUtility.AllocateComputeBuffer(ref Simu_TempPositionBuffer, verticesCount, Marshal.SizeOf(typeof(Vector3)));
            GraphicsUtility.AllocateComputeBuffer(ref Simu_TempVelocityBuffer, verticesCount, Marshal.SizeOf(typeof(Vector3)));
            GraphicsUtility.AllocateComputeBuffer(ref Simu_DisConstraintDeltaPosBuffer, verticesCount, Marshal.SizeOf(typeof(Vector3)));
            GraphicsUtility.AllocateComputeBuffer(ref Simu_BendConstraintDeltaPosBuffer, verticesCount, Marshal.SizeOf(typeof(Vector3)));
            GraphicsUtility.AllocateComputeBuffer(ref Simu_VelocityBuffer, verticesCount, Marshal.SizeOf(typeof(Vector3)));
            GraphicsUtility.AllocateComputeBuffer(ref Simu_MassBuffer, verticesCount, sizeof(float));
            GraphicsUtility.AllocateComputeBuffer(ref Simu_CollisionResultBuffer, verticesCount, Marshal.SizeOf(typeof(ClothCollisionResult)));
            //
            {
                //由于碰撞检测需要在世界坐标中，因此我们需要把顶点转到世界坐标中
                //Mesh_Dynamic_VerticesBuffer的初始值我们在gpu中转换
            }
            Mesh_Dynamic_NormalsBuffer.SetData(Mesh.Normals);
            Mesh_UVBuffer.SetData(Mesh.UVs);
            Mesh_IndicesBuffer.SetData(Mesh.Indices);
            Mesh_BonesPerVertex.SetData(Mesh.BonesPerVertex);
            Mesh_AllBoneWeights.SetData(Mesh.AllBoneWeights);
            Mesh_EdgesPerVertex.SetData(Mesh.EdgesPerVertex);
            Mesh_AllVertexEdges.SetData(Mesh.AllVertexEdges);
            Mesh_SharedEdgesPerVertex.SetData(Mesh.SharedEdgesPerVertex);
            Mesh_AllVertexSharedEdges.SetData(Mesh.AllVertexSharedEdges);
            Mesh_TrianglesPerVertex.SetData(Mesh.TrianglesPerVertex);
            Mesh_AllVertexTriangles.SetData(Mesh.AllVertexTriangles);
            {
                PBDGPUFixedWeight[] _gpuFixedWeights = new PBDGPUFixedWeight[verticesCount];
                for (int i = 0; i < verticesCount; i++)
                {
                    _gpuFixedWeights[i] = new PBDGPUFixedWeight(Mesh.Vertices[i], 0);
                }
                for (int i = 0; i < Mesh.VerticesFixedWeight.Length; i++)
                {
                    var _fw = Mesh.VerticesFixedWeight[i];
                    _gpuFixedWeights[_fw.Index].Weight = _fw.Weight;
                }
                Mesh_VerticesFixedWeight.SetData(_gpuFixedWeights);
            }
            UpdateAnimatedBonesData();
            {
                Simu_VelocityBuffer.SetData(new Vector3[verticesCount]);
            }
        }

        private void ReleaseSimulationBuffers()
        {
            Mesh_Static_VerticesBuffer?.Release();
            Mesh_Dynamic_VerticesBuffer?.Release();
            Mesh_Dynamic_NormalsBuffer?.Release();
            Mesh_UVBuffer?.Release();
            Mesh_IndicesBuffer?.Release();
            //
            Mesh_BonesPerVertex?.Release();
            Mesh_AllBoneWeights?.Release();
            Mesh_EdgesPerVertex?.Release();
            Mesh_AllVertexEdges?.Release();
            Mesh_SharedEdgesPerVertex?.Release();
            Mesh_AllVertexSharedEdges?.Release();
            Mesh_TrianglesPerVertex?.Release();
            Mesh_AllVertexTriangles?.Release();
            Mesh_VerticesFixedWeight?.Release();
            Mesh_BonesLocalToWorldBuffer?.Release();
            //
            Simu_TempPositionBuffer?.Release();
            Simu_TempVelocityBuffer?.Release();
            Simu_DisConstraintDeltaPosBuffer?.Release();
            Simu_BendConstraintDeltaPosBuffer?.Release();
            Simu_VelocityBuffer?.Release();
            Simu_MassBuffer?.Release();
            Simu_CollisionResultBuffer?.Release();
        }
        #endregion

        #region ----Unity----

        private void Start()
        {
            Material = Instantiate(Material);
            //
            SetupBonesData();
            SetupSimulationBuffers();
        }

        private void FixedUpdate()
        {
            UpdateAnimatedBonesData();
        }

        private void OnDestroy()
        {
            ReleaseSimulationBuffers();
            if (Material != null)
            {
                Destroy(Material);
            }
        }
        #endregion
    }
}