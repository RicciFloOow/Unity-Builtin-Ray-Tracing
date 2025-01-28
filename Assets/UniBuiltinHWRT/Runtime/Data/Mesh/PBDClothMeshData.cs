//mainly ref: https://www.cs.toronto.edu/~jacobson/seminar/mueller-et-al-2007.pdf
//注意，damp velocities依赖于求逆矩阵，这在GPU中是红豆泥逆天的行为(至少在对实时性有极高要求的情况下)，因此我们直接跳过这个步骤(可以认为k_damping取为0)
//以下是一些我们需要预计算的数据:
//Distance Constraint:找出所有的边
//Bending Constraint:找出相邻三角形组成的四边形
//此外，需要注意，如果遍历边与四边形，用原子和来对顶点的预估位置处理，那么精度与同步开销都是一个大问题
//因此我们这里与Skinned Mesh的顶点权重一样，按序记录顶点的边与四边形的的索引，以及对应的个数(四边形个数可能为0——没有共享边)
//当然，以上要求我们的模型不能有某个点有特别多的临边的情况(我们假设以该点为一端顶点的边的个数不超过256个)
//因为是Demo，就用三角拓扑了
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UniBuiltinHWRT.Editor;
#endif
using UnityEngine;

namespace UniBuiltinHWRT
{
    [Serializable]
    public struct PBDEdge
    {
        public int Index;//另一端的顶点的索引
        public float BaseLength;

        public PBDEdge(int index, float baseLength)
        {
            Index = index;
            BaseLength = baseLength;
        }
    }

    [Serializable]
    public struct PBDSharedEdge
    {
        //------
        //3, 2
        //1, 4
        //------
        public int Index;//对角的顶点的索引(2)
        public int LTIndex;//3
        public int RBIndex;//4

        public float BaseAngle;

        public PBDSharedEdge(int index, int ltIndex, int rbIndex, float baseAngle)
        {
            Index = index;
            LTIndex = ltIndex;
            RBIndex = rbIndex;
            BaseAngle = baseAngle;
        }
    }

    [Serializable]
    public struct PBDFixedWeight
    {
        public int Index;//顶点索引
        public float Weight;//[0,1], 0表示完全不受骨骼影响, 1表示完全由骨骼影响(可以理解为fixed约束)

        public PBDFixedWeight(int index, float weight)
        {
            Index = index;
            Weight = weight;
        }
    }

    public struct PBDGPUFixedWeight
    {
        public Vector3 Vertex;
        public float Weight;

        public PBDGPUFixedWeight(Vector3 vertex, float weight)
        {
            Vertex = vertex;
            Weight = weight;
        }
    }

    public struct ClothCollisionResult
    {
        public Vector4 HitPoint;
        public Vector3 HitNormal;
    }

    [PreferBinarySerialization]
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "New PBDClothMeshData", menuName = EditorConstantsUtil.MENU_ASSETS + "PBDClothMeshData", order = 1)]
#endif
    public class PBDClothMeshData : ScriptableObject
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;//Tangent就暂时不考虑了
        public Vector2[] UVs;//更多的uv通道也不考虑了
        public int[] Indices;
        //
        public int[] BonesPerVertex;//for skinned mesh, 前24bit是该顶点在AllBoneWeights中对应的起始索引，后8bit是所受bones影响的个数
        public BoneWeight1[] AllBoneWeights;
        public Matrix4x4[] BindPoses;
        //
        public int[] EdgesPerVertex;
        public PBDEdge[] AllVertexEdges;
        //
        public int[] SharedEdgesPerVertex;
        public PBDSharedEdge[] AllVertexSharedEdges;
        //
        public int[] TrianglesPerVertex;//记录每个顶点的：包含该顶点的全部三角形的信息
        public int[] AllVertexTriangles;

        public PBDFixedWeight[] VerticesFixedWeight;//允许不记录全部顶点的fixed约束，这里我们手动配置值

#if UNITY_EDITOR
        [ContextMenu("强制保存")]
        public void ForceSave()
        {
            //
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            //
            Debug.Log("保存成功!");
        }
#endif
    }
}