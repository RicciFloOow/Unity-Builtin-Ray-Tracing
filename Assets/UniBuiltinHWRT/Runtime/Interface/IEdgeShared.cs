using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniBuiltinHWRT
{
    public interface IEdgeShared<T>
    {
        bool Shares(T other);
    }
}