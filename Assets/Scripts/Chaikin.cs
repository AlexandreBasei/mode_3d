using NUnit.Framework;
using System;
using System.Collections.Generic;
//using System.Drawing;
using UnityEngine;

public class Chaikin : MonoBehaviour
{
    [SerializeField] private Vector3 a = new Vector3(0,0,0);
    [SerializeField] private Vector3 b = new Vector3(0,1,0);
    [SerializeField] private Vector3 c = new Vector3(1,1,0);
    [SerializeField] private Vector3 d = new Vector3(1,0,0);

    [SerializeField] private int chaikinIterations = 3;

    private List<Vector3> shape1;
    private List<Vector3> shape2 = new List<Vector3>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        shape1 = new List<Vector3> { a, b, c, d };

        applyChaikin(shape1, chaikinIterations);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (shape1.Count > 0) { 
            for (int i = 0; i < shape1.Count; i++)
            {
                if (i != shape1.Count - 1)
                {
                    
                    Gizmos.DrawLine(shape1[i], shape1[i + 1]);
                }
            }
            Gizmos.DrawLine(shape1[shape1.Count - 1], shape1[0]);
        }

        Gizmos.color = Color.blue;
        if (shape2.Count > 0)
        {
            for (int i = 0; i < shape2.Count - 1; i++)
            {
                if (i != shape2.Count - 1)
                {
                    Gizmos.DrawLine(shape2[i], shape2[i + 1]);
                }
            }

            Gizmos.DrawLine(shape2[shape2.Count - 1], shape2[0]);
        }
    }

    private List<Vector3> chaikin(Vector3 a, Vector3 b)
    {
        Vector3 q = new Vector3(a.x * 0.75f + b.x * 0.25f, a.y * 0.75f + b.y * 0.25f, a.z * 0.75f + b.z * 0.25f);
        Vector3 r = new Vector3(a.x * 0.25f + b.x * 0.75f, a.y * 0.25f + b.y * 0.75f, a.z * 0.25f + b.z * 0.75f);

        return new List<Vector3> { q, r };
    }

    private void applyChaikin(List<Vector3> shape, int iterations)
    {
        List<Vector3> finalShape = new List<Vector3>();

        if (iterations != 0)
        {
            for (int i = 0; i < shape.Count - 1; i++)
            {
                if (i != shape.Count - 1)
                {
                    List<Vector3> chaikinRes = chaikin(shape[i], shape[i + 1]);

                    finalShape.Add(chaikinRes[0]);
                    finalShape.Add(chaikinRes[1]);
                }
            }

            // Dernier segment entre le dernier point et le premier point du maillage
            List<Vector3> chaikinRes2 = chaikin(shape[shape.Count - 1], shape[0]);

            finalShape.Add(chaikinRes2[0]);
            finalShape.Add(chaikinRes2[1]);

            applyChaikin(finalShape, iterations - 1);
        }
        else
        {
            shape2 = shape;
        }
    }
}
