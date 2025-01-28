//需要注意，以下所用的方法都不是高效的，只是展示思路用的
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.Collections;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public class PBDClothMeshConvertor : MonoBehaviour
    {
        private string GetClothMeshDataExportPath(ref Mesh mesh)
        {
            //导出至网格的文件所在位置
            string _meshPath = AssetDatabase.GetAssetPath(mesh);
            string _filePath = Path.GetDirectoryName(_meshPath).Replace("\\", "/");
            string _fileName = Path.GetFileNameWithoutExtension(_meshPath) + "_PBDData.asset";
            return _filePath + "/" + _fileName;
        }

        public void GeneratePBDClothMeshData()
        {
            SkinnedMeshRenderer _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();//我们假设目标网格是skinned mesh
            Mesh _skinnedMesh = _skinnedMeshRenderer.sharedMesh;
            if (_skinnedMesh.subMeshCount > 1)
            {
                //一般来说，如果submesh > 1，那么服饰的“材质”的物理属性就是有较大差异的，且一般是不连通的网格
                Debug.LogError("目标网格submesh数大于1!请分离submesh后再重新创建PBDClothMeshData!");
                return;
            }
            //
            Vector3[] vertices = _skinnedMesh.vertices;
            Vector3[] normals = _skinnedMesh.normals;
            Vector2[] uvs = _skinnedMesh.uv;
            int[] indices = _skinnedMesh.triangles;
            Matrix4x4[] bindPoses = _skinnedMesh.bindposes;
            NativeArray<byte> bonesPerVertex = _skinnedMesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> verticesBoneWeights = _skinnedMesh.GetAllBoneWeights();
            //
            int verticesCount = vertices.Length;
            int trianglesCount = indices.Length / 3;
            //
            int _bonesStartIndex = 0;
            GeoPoint[] geoPoints = new GeoPoint[verticesCount];
            for (int i = 0; i < verticesCount; i++)
            {
                byte _bonesCount = bonesPerVertex[i];
                GeoPoint point = new GeoPoint(vertices[i], normals[i], uvs[i], _bonesCount, _bonesStartIndex);
                geoPoints[i] = point;
                _bonesStartIndex += _bonesCount;
                //我们检查当前点是否与已知的是几何上相同的点
                for (int j = 0; j < i; j++)
                {
                    GeoPoint oPoint = geoPoints[j];
                    if (oPoint.Equals(point))
                    {
                        //是几何上相同的点，理论上这样的顶点所受影响的骨骼与权重都是一致的(只是也重复了)，通常只是为了法线不同
                        //不过UV的正确与否，在这里是无法保证的
                        geoPoints[i] = oPoint;
                        break;
                    }
                }
            }
            //
            List<GeoTriangle> geoTriangles = new List<GeoTriangle>(trianglesCount);
            for (int i = 0; i < trianglesCount; i++)
            {
                int v0Index = indices[3 * i];
                int v1Index = indices[3 * i + 1];
                int v2Index = indices[3 * i + 2];
                //
                var p0 = geoPoints[v0Index];
                var p1 = geoPoints[v1Index];
                var p2 = geoPoints[v2Index];
                //
                if (!p0.Equals(p1) && !p1.Equals(p2) && !p2.Equals(p0))
                {
                    geoTriangles.Add(new GeoTriangle(p0, p1, p2));
                }
            }
            //PBD要求Cloth的网格是一个(可定向的)二维流形，因此下面检测几何上相邻的三角形是否“合法”
            //也即，如果两个三角形中存在两个几何上相同的顶点，那么在给定的定向上，这两个三角形应该是有一个公共边的
            //并且该公共边在两个三角形中对应的向量应该是相反的，所以如果是同向的，那么就是不“合法”的
            trianglesCount = geoTriangles.Count;
            for (int i = 0; i < trianglesCount; i++)
            {
                var _tri = geoTriangles[i];
                for (int j = i + 1; j < trianglesCount; j++)
                {
                    var _oTri = geoTriangles[j];
                    if (_oTri.FakeShares(_tri))
                    {
                        Debug.LogError("发现第" + i + ", " + j + "个三角形有异常, 请检查网格是否合理!");
                        return;
                    }
                }
            }
            //
            List<GeoQuad> _meshQuads = new List<GeoQuad>(trianglesCount + verticesCount + 100);//E=F+V-\chi其中\chi是欧拉示性数，因此我们可以估计服装的边数应该不会大于这个这个数(四边形的数量只会比边数少)
            List<GeoEdge> _meshEdges = new List<GeoEdge>(trianglesCount + verticesCount + 100);
            List<GeoPoint> _reGenPoints = new List<GeoPoint>(verticesCount);
            for (int i = 0; i < trianglesCount; i++)
            {
                var _t1 = geoTriangles[i];
                for (int j = i + 1; j < trianglesCount; j++)
                {
                    var _t2 = geoTriangles[j];
                    var quad = GeoQuad.TryConnect(_t1, _t2);
                    if (quad != null)
                    {
                        _meshQuads.Add(quad);
                    }
                }
                //
                GeoEdge _edge0 = new GeoEdge(_t1.P0, _t1.P1);
                GeoEdge _edge1 = new GeoEdge(_t1.P1, _t1.P2);
                GeoEdge _edge2 = new GeoEdge(_t1.P2, _t1.P0);
                bool _hasEdge0 = false;//我们这里没用Mathematics包
                bool _hasEdge1 = false;
                bool _hasEdge2 = false;
                for (int j = 0; j < _meshEdges.Count; j++)
                {
                    var _e = _meshEdges[j];
                    if (_e.Equals(_edge0))
                    {
                        _hasEdge0 = true;
                    }
                    if (_e.Equals(_edge1))
                    {
                        _hasEdge1 = true;
                    }
                    if (_e.Equals(_edge2))
                    {
                        _hasEdge2 = true;
                    }
                    //
                    if (_hasEdge0 && _hasEdge1 && _hasEdge2)
                    {
                        break;
                    }
                }
                //
                if (!_hasEdge0)
                {
                    _meshEdges.Add(_edge0);
                }
                if (!_hasEdge1)
                {
                    _meshEdges.Add(_edge1);
                }
                if (!_hasEdge2)
                {
                    _meshEdges.Add(_edge2);
                }
                //通过三角形重建顶点
                bool _hasPoint0 = false;
                bool _hasPoint1 = false;
                bool _hasPoint2 = false;
                for (int j = 0; j < _reGenPoints.Count; j++)
                {
                    var _np = _reGenPoints[j];
                    if (_np.Equals(_t1.P0))
                    {
                        _hasPoint0 = true;
                    }
                    if (_np.Equals(_t1.P1))
                    {
                        _hasPoint1 = true;
                    }
                    if (_np.Equals(_t1.P2))
                    {
                        _hasPoint2 = true;
                    }
                    if (_hasPoint0 && _hasPoint1 && _hasPoint2)
                    {
                        break;
                    }
                }
                if (!_hasPoint0)
                {
                    _t1.P0.Index = _reGenPoints.Count;
                    _reGenPoints.Add(_t1.P0);
                }
                if (!_hasPoint1)
                {
                    _t1.P1.Index = _reGenPoints.Count;
                    _reGenPoints.Add(_t1.P1);
                }
                if (!_hasPoint2)
                {
                    _t1.P2.Index = _reGenPoints.Count;
                    _reGenPoints.Add(_t1.P2);
                }
            }
            //
            int[] _newTriangles = new int[trianglesCount * 3];
            int _newVerticesCount = _reGenPoints.Count;
            Vector4[] _newNormalSums = new Vector4[_newVerticesCount];
            //重建三角形(重新获得顶点索引)
            for (int i = 0; i < trianglesCount; i++)
            {
                var _t = geoTriangles[i];
                //int _p0Index = _reGenPoints.IndexOf(_t.P0);
                //int _p1Index = _reGenPoints.IndexOf(_t.P1);
                //int _p2Index = _reGenPoints.IndexOf(_t.P2);
                int _p0Index = _t.P0.Index;
                int _p1Index = _t.P1.Index;
                int _p2Index = _t.P2.Index;
                //
                _newTriangles[i * 3] = _p0Index;
                _newTriangles[i * 3 + 1] = _p1Index;
                _newTriangles[i * 3 + 2] = _p2Index;
                //
                Vector3 _triN = _t.GetTriangleNormal();
                Vector4 _triNW = new Vector4(_triN.x, _triN.y, _triN.z, 1);
                _newNormalSums[_p0Index] += _triNW;
                _newNormalSums[_p1Index] += _triNW;
                _newNormalSums[_p2Index] += _triNW;
            }
            //
            Vector3[] _newVertices = new Vector3[_newVerticesCount];
            Vector3[] _newNormals = new Vector3[_newVerticesCount];
            Vector2[] _newUVs = new Vector2[_newVerticesCount];
            int[] _newBonesPerVertex = new int[_newVerticesCount];
            List<BoneWeight1> _newAllBoneWeights = new List<BoneWeight1>();
            int[] _edgesPerVertex = new int[_newVerticesCount];
            int[] _sharedEdgesPerVertex = new int[_newVerticesCount];
            List<PBDEdge> _allVertexEdges = new List<PBDEdge>();
            List<PBDSharedEdge> _allVertexSharedEdges = new List<PBDSharedEdge>();
            int[] _trianglesPerVertex = new int[_newVerticesCount];
            List<int> _allVertexTriangles = new List<int>();
            //
            int _newBonesStartIndex = 0;
            int _edgeStartIndex = 0;
            int _sharedEdgeStartIndex = 0;
            int _vertTrisStartIndex = 0;
            for (int i = 0; i < _newVerticesCount; i++)
            {
                var _point = _reGenPoints[i];
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
                _newBonesPerVertex[i] = (_newBonesStartIndex << 8) | bWeightedCount;
                _newBonesStartIndex += bWeightedCount;
                //----Edges----
                int _edgeCount = 0;
                for (int j = 0; j < _meshEdges.Count; j++)
                {
                    var _medge = _meshEdges[j];
                    if (_medge.P0.Equals(_point))
                    {
                        //int _oIndex = _reGenPoints.IndexOf(_medge.P1);
                        _allVertexEdges.Add(new PBDEdge(_medge.P1.Index, _medge.BaseLength));
                        _edgeCount++;
                    }
                    else if (_medge.P1.Equals(_point))
                    {
                        _allVertexEdges.Add(new PBDEdge(_medge.P0.Index, _medge.BaseLength));
                        _edgeCount++;
                    }
                }
                _edgesPerVertex[i] = (_edgeStartIndex << 8) | _edgeCount;//这里我们就不做特殊处理了，默认_edgeCount<=255
                _edgeStartIndex += _edgeCount;
                //----Shared Edges----
                int _sharedEdgeCount = 0;
                for (int j = 0; j < _meshQuads.Count; j++)
                {
                    var _mquad = _meshQuads[j];
                    if (_mquad.P1.Equals(_point))
                    {
                        _allVertexSharedEdges.Add(new PBDSharedEdge(_mquad.P2.Index, _mquad.P3.Index, _mquad.P4.Index, _mquad.BaseAngle));
                        _sharedEdgeCount++;
                    }
                    else if (_mquad.P2.Equals(_point))
                    {
                        _allVertexSharedEdges.Add(new PBDSharedEdge(_mquad.P1.Index, _mquad.P4.Index, _mquad.P3.Index, _mquad.BaseAngle));
                        _sharedEdgeCount++;
                    }
                }
                _sharedEdgesPerVertex[i] = (_sharedEdgeStartIndex << 8) | _sharedEdgeCount;
                _sharedEdgeStartIndex += _sharedEdgeCount;
                //----Triangles----
                int _vertTrisCount = 0;
                for (int j = 0; j < trianglesCount; j++)
                {
                    int _v0Index = _newTriangles[j * 3];
                    int _v1Index = _newTriangles[j * 3 + 1];
                    int _v2Index = _newTriangles[j * 3 + 2];
                    //
                    if (_v0Index == i || _v1Index == i || _v2Index == i)
                    {
                        _allVertexTriangles.Add(j);
                        //
                        _vertTrisCount++;
                    }
                }
                _trianglesPerVertex[i] = (_vertTrisStartIndex << 8) | _vertTrisCount;
                _vertTrisStartIndex += _vertTrisCount;
            }
            //
            PBDClothMeshData clothMeshData = ScriptableObject.CreateInstance<PBDClothMeshData>();
            string filePath = GetClothMeshDataExportPath(ref _skinnedMesh);
            AssetDatabase.CreateAsset(clothMeshData, filePath);
            AssetDatabase.ImportAsset(filePath);
            clothMeshData.Vertices = _newVertices;
            clothMeshData.Normals = _newNormals;
            clothMeshData.UVs = _newUVs;
            clothMeshData.Indices = _newTriangles;
            clothMeshData.BonesPerVertex = _newBonesPerVertex;
            clothMeshData.AllBoneWeights = _newAllBoneWeights.ToArray();
            clothMeshData.BindPoses = bindPoses;
            clothMeshData.EdgesPerVertex = _edgesPerVertex;
            clothMeshData.AllVertexEdges = _allVertexEdges.ToArray();
            clothMeshData.SharedEdgesPerVertex = _sharedEdgesPerVertex;
            clothMeshData.AllVertexSharedEdges = _allVertexSharedEdges.ToArray();
            clothMeshData.TrianglesPerVertex = _trianglesPerVertex;
            clothMeshData.AllVertexTriangles = _allVertexTriangles.ToArray();
            clothMeshData.ForceSave();
        }
    }

    [CustomEditor(typeof(PBDClothMeshConvertor))]
    public class PBDClothMeshConvertorEditor : UnityEditor.Editor
    {
        private PBDClothMeshConvertor m_pbdClothMeshConvertor;

        #region ----Unity----
        private void OnEnable()
        {
            m_pbdClothMeshConvertor = (PBDClothMeshConvertor)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("转换PBDClothData"))
            {
                m_pbdClothMeshConvertor.GeneratePBDClothMeshData();
            }
        }
        #endregion
    }
#endif
}