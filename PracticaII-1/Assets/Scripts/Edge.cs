using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge : IComparable<Edge>
{
    public int vertexA;
    public int vertexB;
    public int vertexOther;

    public Edge(int vertexA, int vertexB, int vertexOther)
    {
        this.vertexA = vertexA;
        this.vertexB = vertexB;
        this.vertexOther = vertexOther;
    }

    public int CompareTo(Edge other)
    {
        if (this.vertexA < other.vertexA)
        {
            return -1;
        }
        else if (this.vertexA == other.vertexA)
        {
            if (this.vertexB < other.vertexB)
            {
                return -1;
            }
            else if(this.vertexB == other.vertexB)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        else
        {
            return 1;
        }
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Edge);
    }

    private bool Equals(Edge other)
    {
        if (other == null)
        {
            return false;
        }

        if (vertexA == other.vertexA && vertexB == other.vertexB)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
