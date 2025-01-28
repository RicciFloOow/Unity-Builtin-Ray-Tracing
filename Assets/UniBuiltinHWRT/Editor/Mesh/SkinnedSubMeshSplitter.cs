//我们默认网格的拓扑都是三角形的
//这是将目标子网格分离出来的简易工具(不支持blendshape)
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public class SkinnedSubMeshSplitter : MonoBehaviour
    {
        public SkinnedMeshRenderer SkinnedMesh;

        public int ToSplitSubMeshIndex;

        public string NewMeshName;

        private void RegenerateMeshFromTriangles(GeoPoint[] points, GeoTriangle[] triangles, Matrix4x4[] bindPoses, NativeArray<BoneWeight1> verticesBoneWeights, Transform rootBones, Transform[] bones, Transform newMeshParent, string newMeshName, SubMeshDescriptor[] subMeshDescs)
        {
            List<GeoPoint> _newPoints = new List<GeoPoint>(points.Length);
            for (int i = 0; i < triangles.Length; i++)
            {
                GeoTriangle _t = triangles[i];
                bool _hasPoint0 = false;
                bool _hasPoint1 = false;
                bool _hasPoint2 = false;
                //
                for (int j = 0; j < _newPoints.Count; j++)
                {
                    var p = _newPoints[j];
                    if (p.StrongEquals(_t.P0))
                    {
                        _hasPoint0 = true;
                    }
                    if (p.StrongEquals(_t.P1))
                    {
                        _hasPoint1 = true;
                    }
                    if (p.StrongEquals(_t.P2))
                    {
                        _hasPoint2 = true;
                    }
                    //
                    if (_hasPoint0 && _hasPoint1 && _hasPoint2)
                    {
                        break;
                    }
                }
                //
                if (!_hasPoint0)
                {
                    _t.P0.Index = _newPoints.Count;
                    _newPoints.Add(_t.P0);
                }
                if (!_hasPoint1)
                {
                    _t.P1.Index = _newPoints.Count;
                    _newPoints.Add(_t.P1);
                }
                if (!_hasPoint2)
                {
                    _t.P2.Index = _newPoints.Count;
                    _newPoints.Add(_t.P2);
                }
            }
            //
            int[] _newTriangles = new int[triangles.Length * 3];
            int _newVerticesCount = _newPoints.Count;
            Vector4[] _newNormalSums = new Vector4[_newVerticesCount];
            //
            for (int i = 0; i < triangles.Length; i++)
            {
                var _t = triangles[i];
                //
                int _v0Index = _t.P0.Index;
                int _v1Index = _t.P1.Index;
                int _v2Index = _t.P2.Index;
                //
                _newTriangles[i * 3] = _v0Index;
                _newTriangles[i * 3 + 1] = _v1Index;
                _newTriangles[i * 3 + 2] = _v2Index;
                //
                Vector3 _triN = _t.GetTriangleNormal();
                Vector4 _triNW = new Vector4(_triN.x, _triN.y, _triN.z, 1);
                _newNormalSums[_v0Index] += _triNW;
                _newNormalSums[_v1Index] += _triNW;
                _newNormalSums[_v2Index] += _triNW;
            }
            //
            Vector3[] _newVertices = new Vector3[_newVerticesCount];
            Vector3[] _newNormals = new Vector3[_newVerticesCount];
            Vector2[] _newUVs = new Vector2[_newVerticesCount];
            byte[] _newBonesPerVertex = new byte[_newVerticesCount];
            List<BoneWeight1> _newAllBoneWeights = new List<BoneWeight1>();
            //
            int _newBonesStartIndex = 0;
            for (int i = 0; i < _newVerticesCount; i++)
            {
                var _point = _newPoints[i];
                var _wn = _newNormalSums[i];
                _newVertices[i] = _point.Position;
                _newNormals[i] = new Vector3(_wn.x / _wn.w, _wn.y / _wn.w, _wn.z / _wn.w).normalized;//这里我们就不处理_wn.xyz为0向量的情况了
                _newUVs[i] = _point.UV;
                //----Bones----
                int bPerV = _point.VertexBonesStartCount;
                int bStartIndex = bPerV >> 8;
                int bWeightedCount = bPerV & 0xFF;
                //
                for (int j = 0; j < bWeightedCount; j++)
                {
                    _newAllBoneWeights.Add(verticesBoneWeights[bStartIndex + j]);
                }
                _newBonesPerVertex[i] = (byte)bWeightedCount;
                _newBonesStartIndex += bWeightedCount;
            }
            //
            NativeArray<byte> _newBonesPerVertexNArray = new NativeArray<byte>(_newBonesPerVertex, Allocator.Temp);
            NativeArray<BoneWeight1> _newAllBoneWeightsNArray = new NativeArray<BoneWeight1>(_newAllBoneWeights.ToArray(), Allocator.Temp);
            //
            Mesh newSkinnedMesh = new Mesh()
            {
                name = newMeshName
            };
            newSkinnedMesh.vertices = _newVertices;
            newSkinnedMesh.normals = _newNormals;
            newSkinnedMesh.uv = _newUVs;
            newSkinnedMesh.triangles = _newTriangles;
            if (subMeshDescs != null)
            {
                newSkinnedMesh.SetSubMeshes(subMeshDescs);
            }
            newSkinnedMesh.SetBoneWeights(_newBonesPerVertexNArray, _newAllBoneWeightsNArray);
            newSkinnedMesh.bindposes = bindPoses;
            newSkinnedMesh.RecalculateTangents();
            newSkinnedMesh.RecalculateBounds();
            //
            GameObject _newMeshObj = new GameObject()
            {
                name = newMeshName
            };
            _newMeshObj.transform.SetParent(newMeshParent, false);
            _newMeshObj.transform.SetLocalPositionAndRotation(SkinnedMesh.transform.localPosition, SkinnedMesh.transform.localRotation);
            _newMeshObj.transform.localScale = SkinnedMesh.transform.localScale;
            SkinnedMeshRenderer _newSkinnedMeshRenderer = _newMeshObj.AddComponent<SkinnedMeshRenderer>();
            _newSkinnedMeshRenderer.sharedMesh = newSkinnedMesh;
            _newSkinnedMeshRenderer.rootBone = rootBones;
            _newSkinnedMeshRenderer.bones = bones;
            //
            _newBonesPerVertexNArray.Dispose();
            _newAllBoneWeightsNArray.Dispose();
        }


        private void SplitSkinnedSubMeshes(int targetIndex)
        {
            if (SkinnedMesh == null)
            {
                return;
            }
            Mesh _skinnedMesh = SkinnedMesh.sharedMesh;
            if (_skinnedMesh.subMeshCount <= 1)
            {
                return;//无需分离
            }
            if (targetIndex < 0 || targetIndex  >= _skinnedMesh.subMeshCount)
            {
                Debug.LogError("当前目标分离的submesh索引值:" + targetIndex + "越界");
                return;
            }
            //
            Vector3[] vertices = _skinnedMesh.vertices;
            Vector3[] normals = _skinnedMesh.normals;
            //Vector4[] tangents = _skinnedMesh.tangents;//我们这里就不处理tangent了，就用自带的api重新生成
            Vector2[] uv0 = _skinnedMesh.uv;
            //我们这里就不处理更多的uv通道了
            //Vector2[] uv1 = _skinnedMesh.uv2;
            //Vector2[] uv2 = _skinnedMesh.uv3;
            //Vector2[] uv3 = _skinnedMesh.uv4;
            //Vector2[] uv4 = _skinnedMesh.uv5;
            //Vector2[] uv5 = _skinnedMesh.uv6;
            //Vector2[] uv6 = _skinnedMesh.uv7;
            //Vector2[] uv7 = _skinnedMesh.uv8;
            int[] indices = _skinnedMesh.triangles;
            Matrix4x4[] bindPoses = _skinnedMesh.bindposes;
            NativeArray<byte> bonesPerVertex = _skinnedMesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> verticesBoneWeights = _skinnedMesh.GetAllBoneWeights();
            //
            Transform rootBonesTransform = SkinnedMesh.rootBone;
            Transform[] bonesTransform = SkinnedMesh.bones;
            //这里就不处理blend shape了(毕竟只是demo中所用的工具，需要的话自行实现。需要知道的是，unity fbx exporter导出blend shape的方式也是通过unity的mesh数据转换而来的，而不是直接读取文件拷贝的。此外，对自定义的序列化的网格数据，可以很容易找出比Autodesk内部的API更激进的算法来减少需要记录的三角形数)
            //
            int _meshTrianglesCount = indices.Length / 3;
            int _meshVertexCount = vertices.Length;
            GeoPoint[] _geoVertices = new GeoPoint[_meshVertexCount];
            GeoTriangle[] _geoTriangles = new GeoTriangle[_meshTrianglesCount];
            int _bonesStartIndex = 0;
            for (int i = 0; i < _meshVertexCount; i++)
            {
                byte _bonesCount = bonesPerVertex[i];
                _geoVertices[i] = new GeoPoint(vertices[i], normals[i], uv0[i], _bonesCount, _bonesStartIndex);
                _bonesStartIndex += _bonesCount;
            }
            for (int i = 0; i < _meshTrianglesCount; i++)
            {
                int v0Index = indices[3 * i];
                int v1Index = indices[3 * i + 1];
                int v2Index = indices[3 * i + 2];
                //
                var p0 = _geoVertices[v0Index];
                var p1 = _geoVertices[v1Index];
                var p2 = _geoVertices[v2Index];
                //
                _geoTriangles[i] = new GeoTriangle(p0, p1, p2);
            }
            //
            SubMeshDescriptor subMeshDescriptor = _skinnedMesh.GetSubMesh(targetIndex);
            int _submeshTrianglesCount = subMeshDescriptor.indexCount / 3;
            int _submeshTrianglesStart = subMeshDescriptor.indexStart;
            int _submeshTrianglesStartIndex = _submeshTrianglesStart / 3;
            //
            GeoTriangle[] targetTriangles = new GeoTriangle[_submeshTrianglesCount];
            GeoTriangle[] restTriangles = new GeoTriangle[_meshTrianglesCount - _submeshTrianglesCount];
            Array.Copy(_geoTriangles, _submeshTrianglesStartIndex, targetTriangles, 0, _submeshTrianglesCount);
            if (_submeshTrianglesStartIndex > 0)
            {
                Array.Copy(_geoTriangles, 0, restTriangles, 0, _submeshTrianglesStartIndex);
                Array.Copy(_geoTriangles, _submeshTrianglesStartIndex + _submeshTrianglesCount, restTriangles, _submeshTrianglesStartIndex, _meshTrianglesCount - _submeshTrianglesStartIndex - _submeshTrianglesCount);
            }
            else
            {
                Array.Copy(_geoTriangles, _submeshTrianglesCount, restTriangles, 0, _meshTrianglesCount - _submeshTrianglesCount);
            }
            //
            RegenerateMeshFromTriangles(_geoVertices, targetTriangles, bindPoses, verticesBoneWeights, rootBonesTransform, bonesTransform, SkinnedMesh.transform.parent, NewMeshName, null);
            List<SubMeshDescriptor> restSubMeshDescriptions = new List<SubMeshDescriptor>();
            int _descStartIndex = 0;
            for (int i = 0; i < _skinnedMesh.subMeshCount; i++)
            {
                if (i != targetIndex)
                {
                    SubMeshDescriptor _desc = _skinnedMesh.GetSubMesh(i);
                    restSubMeshDescriptions.Add(new SubMeshDescriptor(_descStartIndex, _desc.indexCount));
                    _descStartIndex += _desc.indexCount;
                }
            }
            RegenerateMeshFromTriangles(_geoVertices, restTriangles, bindPoses, verticesBoneWeights, rootBonesTransform, bonesTransform, SkinnedMesh.transform.parent, SkinnedMesh.name, restSubMeshDescriptions.ToArray());
        }

        public void SplitMeshes()
        {
            SplitSkinnedSubMeshes(ToSplitSubMeshIndex);
        }
    }

    [CustomEditor(typeof(SkinnedSubMeshSplitter))]
    public class SkinnedSubMeshSplitterEditor : UnityEditor.Editor
    {
        private SkinnedSubMeshSplitter m_skinnedSubMeshSplitter;

        #region ----Unity----
        private void OnEnable()
        {
            m_skinnedSubMeshSplitter = (SkinnedSubMeshSplitter)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("分离子网格"))
            {
                m_skinnedSubMeshSplitter.SplitMeshes();
            }
        }
        #endregion
    }
#endif
}