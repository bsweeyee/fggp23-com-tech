using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public static class curveutility
{
    public static float evaluate(float t, DynamicBuffer<CurveBufferData> cbd)
    {
        float a = t * cbd.Length;
        int floorIdx = math.clamp((int)math.floor(a), 0, cbd.Length-1);
        int ceilIdx = math.clamp((int)math.ceil(a), 0, cbd.Length-1);

        float v1 = cbd[floorIdx];
        float v2 = cbd[ceilIdx];

        float frac = math.frac(a);

        // Debug.Log($"{a}, {floorIdx}, {ceilIdx}, {v1}, {v2}");
        return math.lerp(v1, v2, frac);
    }
}
