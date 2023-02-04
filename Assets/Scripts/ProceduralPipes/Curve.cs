using System;
using UnityEngine;

[Serializable]
public class Curve
{
    public Vector3[] points;
    public Vector3 referenceVector = Vector3.up;
    public float length = 1.0f;

    public Curve()
    {
        points = new Vector3[4];
    }

    public Curve(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        points = new Vector3[] { p1, p2, p3, p4 };
    }

    public Vector3 GetPoint(float t) { return (-t*t*t+3*t*t-3*t+1) * points[0] + (3*t*t*t-6*t*t+3*t) * points[1] + (-3*t*t*t+3*t*t) * points[2] + t*t*t * points[3]; }

    public void DataAtPoint(float t, out Vector3 pos, out Vector3 tangent, out Vector3 normal)
    {
        Vector3 l1 = (t*t-2*t+1)*points[0] + 2*(t-t*t)*points[1] + t*t*points[2];
        Vector3 l2 = (t*t-2*t+1)*points[1] + 2*(t-t*t)*points[2] + t*t*points[3];

        pos = l1 + t * (l2 - l1);
        tangent = (l2 - l1).normalized;
        normal = Vector3.Cross(tangent, Vector3.Cross(referenceVector, tangent)).normalized;
    }

    public void UpdateLength(int segmentCount) 
    {
        length = 0;

        for (int i = 0; i < segmentCount; i++)
            length += (GetPoint((i + 1) / segmentCount) - GetPoint(i / segmentCount)).magnitude;
    }
}
