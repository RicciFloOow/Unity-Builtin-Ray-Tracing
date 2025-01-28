using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniBuiltinHWRT
{
    public class GeoPoint : IEquatable<GeoPoint>
    {
        //几何上的点
        public int Index;
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        //
        public int VertexBonesStartCount;
        public int VertexEdgesStartCount;
        public int VertexSharedEdgesStartCount;

        public GeoPoint(Vector3 pos, Vector3 n, Vector2 uv, byte bonesCount, int bonesStartIndex)
        {
            Index = -1;
            Position = pos;
            Normal = n;
            UV = uv;
            //
            VertexBonesStartCount = (bonesStartIndex << 8) | bonesCount;
            VertexEdgesStartCount = 0;
            VertexSharedEdgesStartCount = 0;
        }

        public bool Equals(GeoPoint other)
        {
            Vector3 diff = other.Position - Position;
            return Vector3.Dot(diff, diff) <= GraphicsUtility.K_SamePointSquareDistance;
        }

        public bool StrongEquals(GeoPoint other)
        {
            return Position == other.Position && Normal == other.Normal && UV == other.UV && VertexBonesStartCount == other.VertexBonesStartCount;
        }
    }

    public class GeoTriangle : IEquatable<GeoTriangle>, IEdgeShared<GeoTriangle>
    {
        //几何上的三角形
        public GeoPoint P0;
        public GeoPoint P1;
        public GeoPoint P2;
        //
        public GeoTriangle(GeoPoint p0, GeoPoint p1, GeoPoint p2)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
        }

        public Vector3 GetTriangleNormal()
        {
            Vector3 n = Vector3.Cross(P1.Position - P0.Position, P2.Position - P0.Position);//.normalized的精度太低了，会导致错误得到零向量
            float l = n.magnitude;
            if (l > GraphicsUtility.K_SamePointSquareDistance)
            {
                return n / l;
            }
            return Vector3.zero;
        }

        public bool FakeShares(GeoTriangle other)
        {
            //共享边如果同向就是有问题的
            return (P0.Equals(other.P0) && P1.Equals(other.P1)) || (P1.Equals(other.P0) && P2.Equals(other.P1)) || (P2.Equals(other.P0) && P0.Equals(other.P1))
                || (P0.Equals(other.P1) && P1.Equals(other.P2)) || (P1.Equals(other.P1) && P2.Equals(other.P2)) || (P2.Equals(other.P1) && P0.Equals(other.P2))
                || (P0.Equals(other.P2) && P1.Equals(other.P0)) || (P1.Equals(other.P2) && P2.Equals(other.P0)) || (P2.Equals(other.P2) && P0.Equals(other.P0));
        }

        /// <summary>
        /// 两个三角形共用一边
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Shares(GeoTriangle other)
        {
            return (P0.Equals(other.P1) && P1.Equals(other.P0)) || (P1.Equals(other.P2) && P2.Equals(other.P1)) || (P2.Equals(other.P0) && P0.Equals(other.P2))
                || (P0.Equals(other.P2) && P1.Equals(other.P1)) || (P1.Equals(other.P0) && P2.Equals(other.P2)) || (P2.Equals(other.P1) && P0.Equals(other.P0))
                || (P0.Equals(other.P0) && P1.Equals(other.P2)) || (P1.Equals(other.P1) && P2.Equals(other.P0)) || (P2.Equals(other.P2) && P0.Equals(other.P1));
        }

        public bool Equals(GeoTriangle other)
        {
            return (P0.Equals(other.P0) && P1.Equals(other.P1) && P2.Equals(other.P2))
                || (P0.Equals(other.P1) && P1.Equals(other.P2) && P2.Equals(other.P0))
                || (P0.Equals(other.P2) && P1.Equals(other.P0) && P2.Equals(other.P1));
        }
    }

    public class GeoEdge : IEquatable<GeoEdge>
    {
        public GeoPoint P0;
        public GeoPoint P1;

        public float BaseLength;//"边"的初始长度

        public GeoEdge(GeoPoint p0, GeoPoint p1)
        {
            P0 = p0;
            P1 = p1;
            BaseLength = (p0.Position - p1.Position).magnitude;
        }

        public bool Equals(GeoEdge other)
        {
            return (P0.Equals(other.P0) && P1.Equals(other.P1)) || (P0.Equals(other.P1) && P1.Equals(other.P0));//没有方向要求
        }
    }

    public class GeoQuad
    {
        //我们使用PBD那篇论文中两个共享边的三角形的索引对应的"位置"记法:
        //3, 2,
        //1, 4
        public GeoPoint P1;
        public GeoPoint P2;
        public GeoPoint P3;
        public GeoPoint P4;
        public GeoTriangle Tri1;//1,3,2
        public GeoTriangle Tri2;//1,2,4
        public float BaseAngle;//两个三角形夹出的角度:指定的法线内积的arccos值

        public static GeoQuad TryConnect(GeoTriangle t1, GeoTriangle t2)
        {
            if (t1.P0.Equals(t2.P1) && t1.P1.Equals(t2.P0))
            {
                return new GeoQuad(t1.P0, t1.P1, t2.P2, t1.P2, t2, t1);
            }
            else if (t1.P1.Equals(t2.P2) && t1.P2.Equals(t2.P1))
            {
                return new GeoQuad(t1.P1, t1.P2, t2.P0, t1.P0, t2, t1);
            }
            else if (t1.P2.Equals(t2.P0) && t1.P0.Equals(t2.P2))
            {
                return new GeoQuad(t1.P2, t1.P0, t2.P1, t1.P1, t2, t1);
            }
            else if (t1.P0.Equals(t2.P2) && t1.P1.Equals(t2.P1))
            {
                return new GeoQuad(t1.P0, t1.P1, t2.P0, t1.P2, t2, t1);
            }
            else if (t1.P1.Equals(t2.P0) && t1.P2.Equals(t2.P2))
            {
                return new GeoQuad(t1.P1, t1.P2, t2.P1, t1.P0, t2, t1);
            }
            else if (t1.P2.Equals(t2.P1) && t1.P0.Equals(t2.P0))
            {
                return new GeoQuad(t1.P2, t1.P0, t2.P2, t1.P1, t2, t1);
            }
            else if (t1.P0.Equals(t2.P0) && t1.P1.Equals(t2.P2))
            {
                return new GeoQuad(t1.P0, t1.P1, t2.P1, t1.P2, t2, t1);
            }
            else if (t1.P1.Equals(t2.P1) && t1.P2.Equals(t2.P0))
            {
                return new GeoQuad(t1.P1, t1.P2, t2.P2, t1.P0, t2, t1);
            }
            else if (t1.P2.Equals(t2.P2) && t1.P0.Equals(t2.P1))
            {
                return new GeoQuad(t1.P2, t1.P0, t2.P0, t1.P1, t2, t1);
            }
            return null;
        }

        public GeoQuad(GeoPoint p1, GeoPoint p2, GeoPoint p3, GeoPoint p4, GeoTriangle t1, GeoTriangle t2)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
            //
            Tri1 = t1;//1,3,2
            Tri2 = t2;//1,2,4
            //n1 = Cross(p2 - p3, p1 - p3);
            //n2 = Cross(p2 - p4, p1 - p4);注意，这里不是Cross(p1 - p4, p2 - p4)
            //Acos(Dot(n1, n2))
            BaseAngle = Mathf.Acos(Vector3.Dot(t1.GetTriangleNormal(), -t2.GetTriangleNormal()));
        }
    }
}