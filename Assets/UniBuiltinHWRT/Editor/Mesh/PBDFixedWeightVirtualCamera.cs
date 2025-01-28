//虚拟相机的控制与绘制
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public partial class PBDFixedWeightEditWindow : EditorWindow
    {
        #region ----Mesh Data----
        private ComputeBuffer m_EditPBDClothVerticesBuffer;
        private ComputeBuffer m_EditPBDClothNormalsBuffer;
        private ComputeBuffer m_EditPBDClothUVsBuffer;
        private ComputeBuffer m_EditPBDClothIndicesBuffer;

        private ComputeBuffer m_EditPBDClothEdgesBuffer;

        private ComputeBuffer m_EditPBDClothFixedVertBuffer;
        private ComputeBuffer m_EditPBDClothDispatchedFixedPointBuffer;//fixed顶点的总数(不包括当前选定的顶点)以及需要绘制的顶点的索引
        private ComputeBuffer m_EditPBDClothFixedPointDispatchBuffer;//为了indirect dispatch
        private ComputeBuffer m_EditPBDClothFixedVertIntermediateBuffer;

        private float[] m_verticesFixedWeights;

        private int[] m_fixedVertIntermediateReadBackArray;

        private void SetupMeshDataBuffers()
        {
            if (s_selectPBDClothMeshData == null)
            {
                return;
            }
            if (s_selectPBDClothMeshData.Vertices == null || s_selectPBDClothMeshData.Vertices.Length == 0)
            {
                return;
            }
            if (s_selectPBDClothMeshData.Normals == null || s_selectPBDClothMeshData.Normals.Length == 0)
            {
                return;
            }
            if (s_selectPBDClothMeshData.UVs == null || s_selectPBDClothMeshData.UVs.Length == 0)
            {
                return;
            }
            if (s_selectPBDClothMeshData.Indices == null || s_selectPBDClothMeshData.Indices.Length == 0)
            {
                return;
            }
            if (s_selectPBDClothMeshData.AllVertexEdges == null || s_selectPBDClothMeshData.AllVertexEdges.Length == 0)
            {
                return;
            }
            //
            int verticesCount = s_selectPBDClothMeshData.Vertices.Length;
            int indicesCount = s_selectPBDClothMeshData.Indices.Length;
            int edgesCount = s_selectPBDClothMeshData.AllVertexEdges.Length / 2;//每条几何上的边都被记录了两次
            GraphicsUtility.AllocateComputeBuffer(ref m_EditPBDClothVerticesBuffer, verticesCount, Marshal.SizeOf(typeof(Vector3)));
            GraphicsUtility.AllocateComputeBuffer(ref m_EditPBDClothNormalsBuffer, verticesCount, Marshal.SizeOf(typeof(Vector3)));
            GraphicsUtility.AllocateComputeBuffer(ref m_EditPBDClothUVsBuffer, verticesCount, Marshal.SizeOf(typeof(Vector2)));
            GraphicsUtility.AllocateComputeBuffer(ref m_EditPBDClothIndicesBuffer, indicesCount, sizeof(int));
            GraphicsUtility.AllocateComputeBuffer(ref m_EditPBDClothEdgesBuffer, edgesCount, Marshal.SizeOf(typeof(Vector2Int)));
            GraphicsUtility.AllocateComputeBuffer(ref m_EditPBDClothFixedVertBuffer, verticesCount, sizeof(float));
            GraphicsUtility.AllocateComputeBuffer(ref m_EditPBDClothDispatchedFixedPointBuffer, verticesCount + 1, sizeof(int));
            GraphicsUtility.AllocateComputeBuffer(ref m_EditPBDClothFixedPointDispatchBuffer, 3, sizeof(int), ComputeBufferType.IndirectArguments);
            GraphicsUtility.AllocateComputeBuffer(ref m_EditPBDClothFixedVertIntermediateBuffer, (indicesCount / 3) + 1, sizeof(int));
            //
            m_fixedVertIntermediateReadBackArray = new int[(indicesCount / 3) + 1];
            //
            m_EditPBDClothVerticesBuffer.SetData(s_selectPBDClothMeshData.Vertices);
            m_EditPBDClothNormalsBuffer.SetData(s_selectPBDClothMeshData.Normals);
            m_EditPBDClothUVsBuffer.SetData(s_selectPBDClothMeshData.UVs);
            m_EditPBDClothIndicesBuffer.SetData(s_selectPBDClothMeshData.Indices);
            m_verticesFixedWeights = new float[verticesCount];
            if (s_selectPBDClothMeshData.VerticesFixedWeight != null && s_selectPBDClothMeshData.VerticesFixedWeight.Length > 0)
            {
                var _fixedPoints = s_selectPBDClothMeshData.VerticesFixedWeight;
                for (int i = 0; i < _fixedPoints.Length; i++)
                {
                    var _fixedPoint = _fixedPoints[i];
                    m_verticesFixedWeights[_fixedPoint.Index] = _fixedPoint.Weight;
                }
            }
            m_EditPBDClothFixedVertBuffer.SetData(m_verticesFixedWeights);
            //
            List<Vector2Int> _meshEdges = new List<Vector2Int>();
            bool[] _meshEdgeAuxArray = new bool[edgesCount * 2];
            for (int i = 0; i < verticesCount; i++)
            {
                var _startCount = s_selectPBDClothMeshData.EdgesPerVertex[i];
                int _startIndex = _startCount >> 8;
                int _edgesCount = _startCount & 0XFF;
                for (int j = 0; j < _edgesCount; j++)
                {
                    int _v0SearchIndex = j + _startIndex;
                    int _v0Index = i;
                    int _v1Index = s_selectPBDClothMeshData.AllVertexEdges[_v0SearchIndex].Index;
                    if (!_meshEdgeAuxArray[_v0SearchIndex])
                    {
                        _meshEdgeAuxArray[_v0SearchIndex] = true;
                        //
                        {
                            int _v1StartCount = s_selectPBDClothMeshData.EdgesPerVertex[_v1Index];
                            int _v1StartIndex = _v1StartCount >> 8;
                            int _v1EdgesCount = _v1StartCount & 0XFF;
                            for (int k = 0; k < _v1EdgesCount; k++)
                            {
                                var _vp0 = s_selectPBDClothMeshData.AllVertexEdges[k + _v1StartIndex].Index;
                                if (_vp0 == _v0Index)
                                {
                                    _meshEdgeAuxArray[k + _v1StartIndex] = true;
                                    break;
                                }
                            }
                        }
                        //
                        _meshEdges.Add(new Vector2Int(_v0Index, _v1Index));
                    }
                }
            }
            //
            m_EditPBDClothEdgesBuffer.SetData(_meshEdges);
        }

        private void ReleaseMeshDataBuffers()
        {
            m_EditPBDClothVerticesBuffer?.Release();
            m_EditPBDClothNormalsBuffer?.Release();
            m_EditPBDClothUVsBuffer?.Release();
            m_EditPBDClothIndicesBuffer?.Release();
            m_EditPBDClothEdgesBuffer?.Release();
            m_EditPBDClothFixedVertBuffer?.Release();
            m_EditPBDClothDispatchedFixedPointBuffer?.Release();
            m_EditPBDClothFixedPointDispatchBuffer?.Release();
            m_EditPBDClothFixedVertIntermediateBuffer?.Release();
            //
            m_verticesFixedWeights = null;
        }
        #endregion

        #region ----Skinned Mesh BB----
        private bool CalculateSkinnedMeshBoundingBox(out Vector3 boundingBoxCenter, out Vector3 boundingBoxExtents)
        {
            if (s_selectPBDClothMeshData == null)
            {
                boundingBoxCenter = Vector3.zero;
                boundingBoxExtents = Vector3.zero;
                return false;
            }
            else
            {
                Vector3 _bbMin = float.MaxValue * Vector3.one;
                Vector3 _bbMax = float.MinValue * Vector3.one;
                //
                var _vertices = s_selectPBDClothMeshData.Vertices;
                for (int i = 0; i < _vertices.Length; i++)
                {
                    var _v = _vertices[i];
                    _bbMin = Vector3.Min(_v, _bbMin);
                    _bbMax = Vector3.Max(_v, _bbMax);
                }
                //
                boundingBoxCenter = 0.5f * (_bbMin + _bbMax);
                boundingBoxExtents = 0.5f * (_bbMax - _bbMin);
                return true;
            }
        }
        #endregion

        #region ----Transforms----
        //Demo里就"简单"点
        private Vector3 m_VCam_Position;
        private Quaternion m_VCam_Rotation;
        private Vector3 m_VCam_EulerAngle;

        private float m_TargetSkinnedMeshScale;

        private float m_VCam_NearPlane;

        private float m_VCam_MoveSpeed;

        private float m_VCam_Zoom;

        private Matrix4x4 m_VCam_ViewMatrix;
        private Matrix4x4 m_VCam_ProjectionMatrix;
        private Matrix4x4 m_VCam_VPMatrix;
        private Matrix4x4 m_VCam_CameraToWorld;

        private Vector3 m_VCam_Forward
        {
            get
            {
                return m_VCam_Rotation * Vector3.forward;
            }
        }

        private Vector3 m_VCam_Up
        {
            get
            {
                return m_VCam_Rotation * Vector3.up;
            }
        }

        private Vector3 m_VCam_Right
        {
            get
            {
                return m_VCam_Rotation * Vector3.right;
            }
        }

        private void LookAt(Vector3 desPos, Vector3 sourcePos, ref Quaternion sourceRot)
        {
            Vector3 forward = (desPos - sourcePos).normalized;
            if (forward.magnitude < 10e-8)
            {
                forward = Vector3.forward;
            }
            sourceRot = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void SetupDefaultTransforms()
        {
            m_TargetSkinnedMeshScale = 1;//
            m_VCam_NearPlane = 0.01f;
            m_VCam_MoveSpeed = 200;
            //
            if (CalculateSkinnedMeshBoundingBox(out Vector3 boundingBoxCenter, out Vector3 boundingBoxExtents))
            {
                float _maxSize = boundingBoxExtents.magnitude;
                float _sinHalfTheta = Mathf.Sin(0.5f * Mathf.Deg2Rad * 60);//FOV=60
                m_VCam_Zoom = Mathf.Tan(0.5f * Mathf.Deg2Rad * 60);
                float _distance = _maxSize / (_sinHalfTheta * Mathf.Min(1, 1.7777777778f));//Aspect = 16 / 9
                //
                m_VCam_Position = boundingBoxCenter + Vector3.forward * _distance;
                LookAt(boundingBoxCenter, m_VCam_Position, ref m_VCam_Rotation);//这里其实不用算，直接赋值也可以
            }
            else
            {
                m_VCam_Position = new Vector3(0, 0, 0);
                m_VCam_Rotation = Quaternion.identity;
            }
            m_VCam_EulerAngle = m_VCam_Rotation.eulerAngles;
        }

        private void CalculateVirtualCameraMatrices()
        {
            m_VCam_ViewMatrix = Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(m_VCam_Position, m_VCam_Rotation, Vector3.one).inverse;
            m_VCam_ProjectionMatrix = GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(60, 1.7777777778f, m_VCam_NearPlane, 1000), true);//远裁剪面就不给调整了，没太大必要
            m_VCam_VPMatrix = m_VCam_ProjectionMatrix * m_VCam_ViewMatrix;
            //
            m_VCam_CameraToWorld = Matrix4x4.TRS(m_VCam_Position, m_VCam_Rotation, Vector3.one);
        }
        #endregion

        #region ----Editor Scene View Materials----
        private Material m_SV_SkyboxMat;
        private Material m_SV_PBDClothMat;
        private Material m_SV_FixedPointsMat;

        private void SetupMaterials()
        {
            if (m_SV_SkyboxMat == null)
            {
                m_SV_SkyboxMat = new Material(Shader.Find("UniBuiltinHWRT/Editor/EditPBDEditorWindowSkybox"));
            }
            if (m_SV_PBDClothMat == null)
            {
                m_SV_PBDClothMat = new Material(Shader.Find("UniBuiltinHWRT/Editor/EditPBDSkinnedMesh"));
            }
            if (m_SV_FixedPointsMat == null)
            {
                m_SV_FixedPointsMat = new Material(Shader.Find("UniBuiltinHWRT/Editor/EditPBDDrawFixedPoints"));
            }
        }

        private void ReleaseMaterials()
        {
            if (m_SV_SkyboxMat != null)
            {
                DestroyImmediate(m_SV_SkyboxMat);
            }
            if (m_SV_PBDClothMat != null)
            {
                DestroyImmediate(m_SV_PBDClothMat);
            }
            if (m_SV_FixedPointsMat != null)
            {
                DestroyImmediate(m_SV_FixedPointsMat);
            }
        }
        #endregion

        #region ----Shader Properties----
        private readonly static int k_shaderProperty_Uint_EditPBDClothTriangleCount = Shader.PropertyToID("_EditPBDClothTriangleCount");
        private readonly static int k_shaderProperty_Uint_EditPBDClothVertexCount = Shader.PropertyToID("_EditPBDClothVertexCount");
        private readonly static int k_shaderProperty_Int_EditPBDSelectVertexIndex = Shader.PropertyToID("_EditPBDSelectVertexIndex");
        private readonly static int k_shaderProperty_Float_Zoom = Shader.PropertyToID("_Zoom");
        private readonly static int k_shaderProperty_Float_CamNearPlane = Shader.PropertyToID("_CamNearPlane");
        private readonly static int k_shaderProperty_Vec_EditVirtualCamera_WorldSpacePos = Shader.PropertyToID("_EditVirtualCamera_WorldSpacePos");
        private readonly static int k_shaderProperty_Vec_EditSelectionRayDirection = Shader.PropertyToID("_EditSelectionRayDirection");
        private readonly static int k_shaderProperty_Vec_SelectPointScreenSpacePos = Shader.PropertyToID("_SelectPointScreenSpacePos");
        private readonly static int k_shaderProperty_Matrix_EditVirtualCamera_CameraToWorld = Shader.PropertyToID("_EditVirtualCamera_CameraToWorld");
        private readonly static int k_shaderProperty_Matrix_EditVirtualCamera_VPMatrix = Shader.PropertyToID("_EditVirtualCamera_VPMatrix");
        private readonly static int k_shaderProperty_Matrix_meshObj2WorldMatrix = Shader.PropertyToID("_meshObj2WorldMatrix");
        private readonly static int k_shaderProperty_Tex_EditSkyboxCubeTex = Shader.PropertyToID("_EditSkyboxCubeTex");
        private readonly static int k_shaderProperty_Tex_VirtualCameraDepth_RT = Shader.PropertyToID("_VirtualCameraDepth_RT");
        private readonly static int k_shaderProperty_Tex_VirtualCameraFixedPoints_RT = Shader.PropertyToID("_VirtualCameraFixedPoints_RT");
        private readonly static int k_shaderProperty_Tex_RW_VirtualCameraFixedPoints_RT = Shader.PropertyToID("RW_VirtualCameraFixedPoints_RT");
        private readonly static int k_shaderProperty_Buffer_EditPBDSkinnedMeshVerticesBuffer = Shader.PropertyToID("_EditPBDSkinnedMeshVerticesBuffer");
        private readonly static int k_shaderProperty_Buffer_EditPBDSkinnedMeshNormalsBuffer = Shader.PropertyToID("_EditPBDSkinnedMeshNormalsBuffer");
        private readonly static int k_shaderProperty_Buffer_EditPBDSkinnedMeshUVsBuffer = Shader.PropertyToID("_EditPBDSkinnedMeshUVsBuffer");
        private readonly static int k_shaderProperty_Buffer_EditPBDSkinnedMeshIndicesBuffer = Shader.PropertyToID("_EditPBDSkinnedMeshIndicesBuffer");
        private readonly static int k_shaderProperty_Buffer_EditPBDSkinnedMeshEdgesBuffer = Shader.PropertyToID("_EditPBDSkinnedMeshEdgesBuffer");
        private readonly static int k_shaderProperty_Buffer_EditPBDFixedVertBuffer = Shader.PropertyToID("_EditPBDFixedVertBuffer");
        private readonly static int k_shaderProperty_Buffer_EditPBDDispatchedFixedPointBuffer = Shader.PropertyToID("_EditPBDDispatchedFixedPointBuffer");
        private readonly static int k_shaderProperty_Buffer_EditPBDClothFixedVertBuffer = Shader.PropertyToID("_EditPBDClothFixedVertBuffer");
        private readonly static int k_shaderProperty_Buffer_RW_ResultBuffer = Shader.PropertyToID("RW_ResultBuffer");
        private readonly static int k_shaderProperty_Buffer_RW_EditPBDDispatchedFixedPointBuffer = Shader.PropertyToID("RW_EditPBDDispatchedFixedPointBuffer");
        private readonly static int k_shaderProperty_Buffer_RW_EditFixedPointDispatchBuffer = Shader.PropertyToID("RW_EditFixedPointDispatchBuffer");
        #endregion

        #region ----Editor Res----
        private Cubemap m_skyboxCubemap;

        private ComputeShader m_vertexSelectionCS;
        private ComputeShader m_fixedPointsDrawCS;

        private void LoadEditAdditionalRenderingRes()
        {
            if (m_skyboxCubemap == null)
            {
                m_skyboxCubemap = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/UniBuiltinHWRT/Demo/DemoRes/UniRTRes/HDRI/MuirWoodWhiteBalanced.exr") as Cubemap;
                //
                if (m_skyboxCubemap == null)
                {
                    Debug.LogWarning("未能在指定地址找到所需的纹理:Assets/UniBuiltinHWRT/Demo/DemoRes/UniRTRes/HDRI/MuirWoodWhiteBalanced.exr");
                }
            }
            if (m_vertexSelectionCS == null)
            {
                m_vertexSelectionCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/UniBuiltinHWRT/Shader/Editor/PBDMesh/EditPBDVertexSelection.compute") as ComputeShader;
                //
                if (m_vertexSelectionCS == null)
                {
                    Debug.LogWarning("未能在指定地址找到所需的CS:Assets/UniBuiltinHWRT/Shader/Editor/PBDMesh/EditPBDVertexSelection.compute");
                }
            }
            if (m_fixedPointsDrawCS == null)
            {
                m_fixedPointsDrawCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/UniBuiltinHWRT/Shader/Editor/PBDMesh/EditPBDFixedPoints.compute") as ComputeShader;
                //
                if (m_fixedPointsDrawCS == null)
                {
                    Debug.LogWarning("未能在指定地址找到所需的CS:Assets/UniBuiltinHWRT/Shader/Editor/PBDMesh/EditPBDFixedPoints.compute");
                }
            }
        }

        private void ReleaseEditAdditionalRenderingRes()
        {
            m_skyboxCubemap = null;
            m_vertexSelectionCS = null;
            m_fixedPointsDrawCS = null;
        }
        #endregion

        #region ----Virtual Camera Settings GUI----
        private void DrawVirtualCameraSettingsGUI()
        {
            m_TargetSkinnedMeshScale = EditorGUILayout.Slider("网格缩放:", m_TargetSkinnedMeshScale, 0, 10);
            //
            m_VCam_NearPlane = EditorGUILayout.Slider("近裁剪面:", m_VCam_NearPlane, 0.0001f, 1);
            //
            m_VCam_MoveSpeed = EditorGUILayout.Slider("相机速度:", m_VCam_MoveSpeed, 0.1f, 1000);
            m_VCam_Position = EditorGUILayout.Vector3Field("相机坐标:", m_VCam_Position);
        }
        #endregion

        #region ----Vertex Selection----
        //由于只是一个demo，我们不追求性能，因此也就不对网格建BVH树来加速检测选中的顶点了
        //并且这里我们只做能选中一个顶点的
        private bool m_isCheckingSelectVertex;

        private int m_selectVertexIndex;

        private void EditWindowPosToRay(Rect rect, Vector2 pos, out Vector3 rayDir)
        {
            //ray origin就用相机位置即可
            Vector2 _size = rect.size;
            float aspectRatio = _size.x / _size.y;
            //需要注意GUI的(0, 0)是在左上角的而不是左下角的
            pos = new Vector2((pos.x - rect.min.x) / _size.x, 1 - (pos.y - rect.min.y) / _size.y);
            pos = m_VCam_Zoom * new Vector2(pos.x * 2 - 1, pos.y * 2 - 1);
            Vector3 viewDir = new Vector3(pos.x * aspectRatio, pos.y, 1).normalized;
            Vector4 _rayDir = new Vector4(viewDir.x, viewDir.y, viewDir.z, 0);
            _rayDir = m_VCam_CameraToWorld * _rayDir;
            rayDir = new Vector3(_rayDir.x, _rayDir.y, _rayDir.z).normalized;
        }

        private void ExecuteEditInitResultKernel(ComputeShader cs)
        {
            int _kernelIndex = cs.FindKernel("EditInitResultKernel");
            cs.SetBuffer(_kernelIndex, k_shaderProperty_Buffer_RW_ResultBuffer, m_EditPBDClothFixedVertIntermediateBuffer);
            cs.Dispatch(_kernelIndex, 1, 1, 1);
        }

        private void ExecuteEditVertexSelectionKernel(ComputeShader cs, Vector3 rayDir)
        {
            int _triCount = m_EditPBDClothIndicesBuffer.count / 3;
            int _kernelIndex = cs.FindKernel("EditVertexSelectionKernel");
            cs.GetKernelThreadGroupSizes(_kernelIndex, out uint x, out uint y, out uint z);
            cs.SetInt(k_shaderProperty_Uint_EditPBDClothTriangleCount, _triCount);
            cs.SetVector(k_shaderProperty_Vec_EditSelectionRayDirection, rayDir);
            cs.SetVector(k_shaderProperty_Vec_EditVirtualCamera_WorldSpacePos, m_VCam_Position);
            cs.SetBuffer(_kernelIndex, k_shaderProperty_Buffer_EditPBDSkinnedMeshVerticesBuffer, m_EditPBDClothVerticesBuffer);
            cs.SetBuffer(_kernelIndex, k_shaderProperty_Buffer_EditPBDSkinnedMeshIndicesBuffer, m_EditPBDClothIndicesBuffer);
            cs.SetBuffer(_kernelIndex, k_shaderProperty_Buffer_RW_ResultBuffer, m_EditPBDClothFixedVertIntermediateBuffer);
            cs.Dispatch(_kernelIndex, Mathf.CeilToInt(_triCount / (float)x), 1, 1);
        }

        private void TryFindNearestPoint(ref float rayLength, Vector3 rayDir, Vector3 rayOrigin, Vector3 v0, Vector3 v1, Vector3 v2, int v0Index, int v1Index, int v2Index)
        {
            Vector3 edgeAB = v1 - v0;
            Vector3 edgeAC = v2 - v0;
            //
            Vector3 normalVec = Vector3.Cross(edgeAB, edgeAC);
            Vector3 ao = rayOrigin - v0;
            //
            float determinant = -Vector3.Dot(rayDir, normalVec);
            float invDet = 1 / determinant;
            float dst = Vector3.Dot(ao, normalVec) * invDet;
            //
            if (dst < rayLength)
            {
                rayLength = dst;
                //
                Vector3 hitPos = rayOrigin + rayDir * dst;
                //
                float d0 = (v0 - hitPos).sqrMagnitude;
                float d1 = (v1 - hitPos).sqrMagnitude;
                float d2 = (v2 - hitPos).sqrMagnitude;
                //
                float _vertMin = d0;
                m_selectVertexIndex = v0Index;
                if (d1 < _vertMin)
                {
                    _vertMin = d1;
                    m_selectVertexIndex = v1Index;
                }
                if (d2 < _vertMin)
                {
                    m_selectVertexIndex = v2Index;
                }
            }
        }


        private void TryFindSelectedVertex(Vector3 rayDir)
        {
            ExecuteEditInitResultKernel(m_vertexSelectionCS);
            ExecuteEditVertexSelectionKernel(m_vertexSelectionCS, rayDir);
            //需要异步回读的话要么用editorcoroutines包，要么用EditorApplication.QueuePlayerLoopUpdate自行实现
            //我们这里同步回读
            m_EditPBDClothFixedVertIntermediateBuffer.GetData(m_fixedVertIntermediateReadBackArray);
            //
            int _intersectTriangles = m_fixedVertIntermediateReadBackArray[0];
            if (_intersectTriangles > 0)
            {
                Vector3 _nomRayDir = rayDir.normalized;
                float _nearestTriangleDis = float.MaxValue;
                for (int i = 0; i < _intersectTriangles; i++)
                {
                    int _triIndex = m_fixedVertIntermediateReadBackArray[1 + i];
                    //
                    int _v0Index = s_selectPBDClothMeshData.Indices[_triIndex * 3];
                    int _v1Index = s_selectPBDClothMeshData.Indices[_triIndex * 3 + 1];
                    int _v2Index = s_selectPBDClothMeshData.Indices[_triIndex * 3 + 2];
                    //
                    Vector3 _v0 = s_selectPBDClothMeshData.Vertices[_v0Index];
                    Vector3 _v1 = s_selectPBDClothMeshData.Vertices[_v1Index];
                    Vector3 _v2 = s_selectPBDClothMeshData.Vertices[_v2Index];
                    //
                    TryFindNearestPoint(ref _nearestTriangleDis, _nomRayDir, m_VCam_Position, _v0, _v1, _v2, _v0Index, _v1Index, _v2Index);
                }
                //
                m_SelectedFixedPointWeight = m_verticesFixedWeights[m_selectVertexIndex];
            }
            else
            {
                m_selectVertexIndex = -1;
                m_SelectedFixedPointWeight = 0;
            }
            //
            m_isCheckingSelectVertex = false;
        }
        #endregion

        #region ----Virtual Camera Control----
        private Vector2 m_lastFrameRightMousePosition;
        private Vector2 m_lastFrameMiddleMousePosition;

        private double m_lastFrameTime;

        private void OnCamRotate(Vector2 delta)
        {
            delta *= -0.1f;
            m_VCam_EulerAngle += new Vector3(delta.y, delta.x, 0);
            //
            m_VCam_Rotation = Quaternion.Euler(m_VCam_EulerAngle);
        }

        private void OnCamMove(Vector2 delta)
        {
            delta *= 0.01f;
            //移动相机
            m_VCam_Position += (-delta.x * m_VCam_Right + delta.y * m_VCam_Up);
        }

        private void OnControlVirtualCamera(Rect sceneViewRect)
        {
            bool _needRepaint = false;
            float deltaTime = (float)(EditorApplication.timeSinceStartup - m_lastFrameTime);
            Vector2 _mousePosition = Event.current.mousePosition;

            bool isCursorInRect = sceneViewRect.Contains(_mousePosition);
            //鼠标在纹理绘制区域内
            if ((Event.current.type == EventType.MouseDown) && isCursorInRect)
            {
                if (Event.current.isMouse)
                {
                    if (Event.current.button == 1)
                    {
                        //鼠标右键
                        m_lastFrameRightMousePosition = _mousePosition;
                    }
                    else if (Event.current.button == 2)
                    {
                        //鼠标中键
                        m_lastFrameMiddleMousePosition = _mousePosition;
                    }
                    else if (Event.current.button == 0)
                    {
                        //鼠标左键
                        if (!m_isCheckingSelectVertex)
                        {
                            m_isCheckingSelectVertex = true;
                            //计算最接近的顶点(先计算ray)
                            EditWindowPosToRay(sceneViewRect, _mousePosition, out Vector3 rayDir);
                            //
                            TryFindSelectedVertex(rayDir);
                            _needRepaint = true;
                        }
                    }
                }
            }
            else if ((Event.current.type == EventType.MouseDrag) && isCursorInRect)
            {
                if (Event.current.isMouse)
                {
                    if (Event.current.button == 1)
                    {
                        //鼠标右键
                        Vector2 deltaMousePosition = _mousePosition - m_lastFrameRightMousePosition;
                        OnCamRotate(deltaMousePosition);
                        _needRepaint = true;
                        m_lastFrameRightMousePosition = _mousePosition;
                    }
                    else if (Event.current.button == 2)
                    {
                        //鼠标中键
                        Vector2 deltaMousePosition = _mousePosition - m_lastFrameMiddleMousePosition;
                        OnCamMove(deltaMousePosition);
                        _needRepaint = true;
                        m_lastFrameMiddleMousePosition = _mousePosition;
                    }
                }
            }
            if (Event.current.Equals(Event.KeyboardEvent("W")))//W
            {
                //ref: https://docs.unity3d.com/ScriptReference/Event.KeyboardEvent.html
                m_VCam_Position += m_VCam_Forward * m_VCam_MoveSpeed * deltaTime;
                _needRepaint = true;
            }
            if (Event.current.Equals(Event.KeyboardEvent("A")))//A
            {
                m_VCam_Position -= m_VCam_Right * m_VCam_MoveSpeed * deltaTime;
                _needRepaint = true;
            }
            if (Event.current.Equals(Event.KeyboardEvent("S")))//S
            {
                m_VCam_Position -= m_VCam_Forward * m_VCam_MoveSpeed * deltaTime;
                _needRepaint = true;
            }
            if (Event.current.Equals(Event.KeyboardEvent("D")))//D
            {
                m_VCam_Position += m_VCam_Right * m_VCam_MoveSpeed * deltaTime;
                _needRepaint = true;
            }
            //
            if (_needRepaint)
            {
                Repaint();
            }
            //
            m_lastFrameTime = EditorApplication.timeSinceStartup;
        }
        #endregion

        #region ----Virtual Camera Rendering----
        private void ExecuteInitDispatchedFixedPointBufferKernel(ref CommandBuffer cmd, ComputeShader cs)
        {
            int _kernelIndex = cs.FindKernel("InitDispatchedFixedPointBufferKernel");
            cmd.SetComputeBufferParam(cs, _kernelIndex, k_shaderProperty_Buffer_RW_EditPBDDispatchedFixedPointBuffer, m_EditPBDClothDispatchedFixedPointBuffer);
            cmd.DispatchCompute(cs, _kernelIndex, 1, 1, 1);
        }

        private void ExecuteDispatchToDrawFixedPointKernel(ref CommandBuffer cmd, ComputeShader cs, Matrix4x4 obj2World)
        {
            int verticesCount = m_EditPBDClothVerticesBuffer.count;
            int _kernelIndex = cs.FindKernel("DispatchToDrawFixedPointKernel");
            cs.GetKernelThreadGroupSizes(_kernelIndex, out uint x, out uint y, out uint z);
            cmd.SetComputeIntParam(cs, k_shaderProperty_Uint_EditPBDClothVertexCount, verticesCount);
            cmd.SetComputeIntParam(cs, k_shaderProperty_Int_EditPBDSelectVertexIndex, m_selectVertexIndex);
            cmd.SetComputeMatrixParam(cs, k_shaderProperty_Matrix_EditVirtualCamera_VPMatrix, m_VCam_VPMatrix);
            cmd.SetComputeMatrixParam(cs, k_shaderProperty_Matrix_meshObj2WorldMatrix, obj2World);
            cmd.SetComputeTextureParam(cs, _kernelIndex, k_shaderProperty_Tex_VirtualCameraDepth_RT, m_VCamDepth_Handle);
            cmd.SetComputeBufferParam(cs, _kernelIndex, k_shaderProperty_Buffer_EditPBDSkinnedMeshVerticesBuffer, m_EditPBDClothVerticesBuffer);
            cmd.SetComputeBufferParam(cs, _kernelIndex, k_shaderProperty_Buffer_EditPBDClothFixedVertBuffer, m_EditPBDClothFixedVertBuffer);
            cmd.SetComputeBufferParam(cs, _kernelIndex, k_shaderProperty_Buffer_RW_EditPBDDispatchedFixedPointBuffer, m_EditPBDClothDispatchedFixedPointBuffer);
            cmd.DispatchCompute(cs, _kernelIndex, Mathf.CeilToInt(verticesCount / (float)x), 1, 1);
        }

        private void ExecuteIndirectDispatchKernel(ref CommandBuffer cmd, ComputeShader cs)
        {
            int _kernelIndex = cs.FindKernel("IndirectDispatchKernel");
            cmd.SetComputeBufferParam(cs, _kernelIndex, k_shaderProperty_Buffer_EditPBDDispatchedFixedPointBuffer, m_EditPBDClothDispatchedFixedPointBuffer);
            cmd.SetComputeBufferParam(cs, _kernelIndex, k_shaderProperty_Buffer_RW_EditFixedPointDispatchBuffer, m_EditPBDClothFixedPointDispatchBuffer);
            cmd.DispatchCompute(cs, _kernelIndex, 1, 1, 1);
        }

        private void ExecuteDrawFixedPointsKernel(ref CommandBuffer cmd, ComputeShader cs, Matrix4x4 obj2World)
        {
            int _kernelIndex = cs.FindKernel("DrawFixedPointsKernel");
            cmd.SetComputeMatrixParam(cs, k_shaderProperty_Matrix_EditVirtualCamera_VPMatrix, m_VCam_VPMatrix);
            cmd.SetComputeMatrixParam(cs, k_shaderProperty_Matrix_meshObj2WorldMatrix, obj2World);
            cmd.SetComputeBufferParam(cs, _kernelIndex, k_shaderProperty_Buffer_EditPBDDispatchedFixedPointBuffer, m_EditPBDClothDispatchedFixedPointBuffer);
            cmd.SetComputeBufferParam(cs, _kernelIndex, k_shaderProperty_Buffer_EditPBDSkinnedMeshVerticesBuffer, m_EditPBDClothVerticesBuffer);
            cmd.SetRenderTarget(m_VCamFixedPoints_Handle);
            cmd.SetComputeTextureParam(cs, _kernelIndex, k_shaderProperty_Tex_RW_VirtualCameraFixedPoints_RT, m_VCamFixedPoints_Handle);
            cmd.DispatchCompute(cs, _kernelIndex, m_EditPBDClothFixedPointDispatchBuffer, 0);
        }

        private void OnRenderingEditorVirtualCamera()
        {
            CalculateVirtualCameraMatrices();
            //
            CommandBuffer editCmdBuffer = new CommandBuffer()
            {
                name = "PBD Fixed Weight Editor View Pass"
            };
            editCmdBuffer.SetRenderTarget(m_VCamColor_Handle, m_VCamDepth_Handle);
            editCmdBuffer.ClearRenderTarget(true, true, Color.clear);
            //
            {
                //skybox:这里偷点懒，直接用采样cubemap的
                MaterialPropertyBlock _matPropertyBlock = new MaterialPropertyBlock();
                _matPropertyBlock.SetFloat(k_shaderProperty_Float_Zoom, m_VCam_Zoom);
                _matPropertyBlock.SetMatrix(k_shaderProperty_Matrix_EditVirtualCamera_CameraToWorld, m_VCam_CameraToWorld);
                _matPropertyBlock.SetTexture(k_shaderProperty_Tex_EditSkyboxCubeTex, m_skyboxCubemap);
                editCmdBuffer.DrawProcedural(Matrix4x4.identity, m_SV_SkyboxMat, 0, MeshTopology.Triangles, 3, 1, _matPropertyBlock);
            }
            //
            Matrix4x4 _meshObj2WorldMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * m_TargetSkinnedMeshScale);
            //
            if (m_EditPBDClothIndicesBuffer != null)
            {
                //
                {
                    MaterialPropertyBlock _matPropertyBlock = new MaterialPropertyBlock();
                    _matPropertyBlock.SetVector(k_shaderProperty_Vec_EditVirtualCamera_WorldSpacePos, m_VCam_Position);
                    _matPropertyBlock.SetMatrix(k_shaderProperty_Matrix_EditVirtualCamera_VPMatrix, m_VCam_VPMatrix);
                    _matPropertyBlock.SetBuffer(k_shaderProperty_Buffer_EditPBDSkinnedMeshVerticesBuffer, m_EditPBDClothVerticesBuffer);
                    _matPropertyBlock.SetBuffer(k_shaderProperty_Buffer_EditPBDSkinnedMeshNormalsBuffer, m_EditPBDClothNormalsBuffer);
                    _matPropertyBlock.SetBuffer(k_shaderProperty_Buffer_EditPBDSkinnedMeshUVsBuffer, m_EditPBDClothUVsBuffer);
                    _matPropertyBlock.SetBuffer(k_shaderProperty_Buffer_EditPBDSkinnedMeshIndicesBuffer, m_EditPBDClothIndicesBuffer);
                    editCmdBuffer.DrawProcedural(_meshObj2WorldMatrix, m_SV_PBDClothMat, 0, MeshTopology.Triangles, m_EditPBDClothIndicesBuffer.count, 1, _matPropertyBlock);
                }
                //
                {
                    MaterialPropertyBlock _matPropertyBlock = new MaterialPropertyBlock();
                    _matPropertyBlock.SetFloat(k_shaderProperty_Float_CamNearPlane, m_VCam_NearPlane);
                    _matPropertyBlock.SetMatrix(k_shaderProperty_Matrix_EditVirtualCamera_VPMatrix, m_VCam_VPMatrix);
                    _matPropertyBlock.SetBuffer(k_shaderProperty_Buffer_EditPBDSkinnedMeshVerticesBuffer, m_EditPBDClothVerticesBuffer);
                    _matPropertyBlock.SetBuffer(k_shaderProperty_Buffer_EditPBDSkinnedMeshEdgesBuffer, m_EditPBDClothEdgesBuffer);
                    editCmdBuffer.DrawProcedural(_meshObj2WorldMatrix, m_SV_PBDClothMat, 1, MeshTopology.Lines, m_EditPBDClothEdgesBuffer.count * 2, 1, _matPropertyBlock);
                }
            }
            //绘制fixed的顶点(区分当前选中的以及已经赋值的)
            {
                //绘制选中点
                if (m_selectVertexIndex >= 0)
                {
                    Vector3 _vertPosObj = s_selectPBDClothMeshData.Vertices[m_selectVertexIndex];
                    Vector4 vertPosObj = new Vector4(_vertPosObj.x, _vertPosObj.y, _vertPosObj.z, 1);
                    vertPosObj = m_VCam_VPMatrix * (_meshObj2WorldMatrix * vertPosObj);
                    //
                    MaterialPropertyBlock _matPropertyBlock = new MaterialPropertyBlock();
                    _matPropertyBlock.SetVector(k_shaderProperty_Vec_SelectPointScreenSpacePos, vertPosObj);
                    editCmdBuffer.DrawProcedural(Matrix4x4.identity, m_SV_FixedPointsMat, 1, MeshTopology.Triangles, 6, 1, _matPropertyBlock);
                }
                //
                {
                    editCmdBuffer.SetRenderTarget(m_VCamFixedPoints_Handle);
                    editCmdBuffer.ClearRenderTarget(false, true, Color.clear);
                }
                //绘制已经fixed的点
                if (s_selectPBDClothMeshData.VerticesFixedWeight != null && s_selectPBDClothMeshData.VerticesFixedWeight.Length > 0)
                {
                    //
                    ExecuteInitDispatchedFixedPointBufferKernel(ref editCmdBuffer, m_fixedPointsDrawCS);
                    ExecuteDispatchToDrawFixedPointKernel(ref editCmdBuffer, m_fixedPointsDrawCS, _meshObj2WorldMatrix);
                    ExecuteIndirectDispatchKernel(ref editCmdBuffer, m_fixedPointsDrawCS);
                    ExecuteDrawFixedPointsKernel(ref editCmdBuffer, m_fixedPointsDrawCS, _meshObj2WorldMatrix);
                    //
                    {
                        editCmdBuffer.SetRenderTarget(m_VCamColor_Handle);
                        MaterialPropertyBlock _matPropertyBlock = new MaterialPropertyBlock();
                        _matPropertyBlock.SetTexture(k_shaderProperty_Tex_VirtualCameraFixedPoints_RT, m_VCamFixedPoints_Handle);
                        editCmdBuffer.DrawProcedural(Matrix4x4.identity, m_SV_FixedPointsMat, 0, MeshTopology.Triangles, 3, 1, _matPropertyBlock);
                    }
                }
            }
            //
            Graphics.ExecuteCommandBuffer(editCmdBuffer);
        }
        #endregion
    }
#endif
}