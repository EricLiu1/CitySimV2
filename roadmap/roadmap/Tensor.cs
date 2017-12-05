using System;
using System.Collections.Generic;
using System.Numerics;
using Priority_Queue;

public struct Tensor
{
    // A tensor is a 2x2 symmetric and traceless matrix of the form
    // R * | cos(2theta)  sin(2theta) |  = | a b |
    //     | sin(2theta) -cos(2theta) |    | _ _ |
    // where R >= 0 and theta is [0, 2pi)

    public  double A;
    public  double B;
    public int type;
    public Vector2 center2;

    public Tensor(double a, double b, int kind, Vector2 center)
    {
        A = a;
        B = b;
        type = kind;
        center2 = center;
    }

    public static Tensor FromRTheta(double r, double theta, Vector2 center)
    {
        return Normalize(new Tensor(r * Math.Cos(2 * theta), r * Math.Sin(2 * theta), 0, center));
    }

    public static Tensor FromXY(Vector2 pos, Vector2 center)
    {
        Vector2 xy = pos - center;

        var xy2 = -2 * xy.X * xy.Y;
        var diffSquares = xy.Y * xy.Y - xy.X * xy.X;
        return Normalize(new Tensor(diffSquares, xy2, 1, center));
    }

    public static Tensor Normalize(Tensor tensor)
    {
        var l = Math.Sqrt(tensor.A * tensor.A + tensor.B * tensor.B);
        if (Math.Abs(l) < float.Epsilon)
            return new Tensor(0, 0, tensor.type, tensor.center2);

        return new Tensor(tensor.A / l, tensor.B / l, tensor.type, tensor.center2);

    }

    public static Tensor operator +(Tensor left, Tensor right)
    {
        return new Tensor(left.A + right.A, left.B + right.B, right.type, right.center2);
    }

    public static Tensor operator -(Tensor t)
    {
        return new Tensor(-t.A, -t.B, t.type, t.center2);
    }

    public static Tensor operator *(double left, Tensor right)
    {
        return new Tensor(left * right.A, left * right.B, right.type, right.center2);
    }

    //Eigen calculation based on http://www.math.harvard.edu/archive/21b_fall_04/exhibits/2dmatrices/index.html
    public void EigenValues(out double e1, out double e2)
    {
        var eval = Math.Sqrt(A * A + B * B);

        e1 = eval;
        e2 = -eval;
    }
     
    public void EigenVectors(out Vector2 major, out Vector2 minor)
    {
        if (Math.Abs(B) < 0.0000001f)
        {
            if (Math.Abs(A) < 0.0000001f)
            {
                major = new Vector2(0,0);
                minor = new Vector2(0,0);
            }
            else
            {
                major = new Vector2(1, 0);
                minor = new Vector2(0, 1);
            }
        }
        else
        {
            double e1, e2;
            EigenValues(out e1, out e2);

            major = new Vector2((float)B, (float)(e1 - A));
            minor = new Vector2((float)B, (float)(e2 - A));
        }
    }

    public Vector2 Sample()
    {
        return new Vector2((float)A, (float)B);
    }

    public static Tensor Sample(Vector2 pos, Vector2 prev_dir, List<Tensor> weightedavgs, bool tracingMajor)
    {
        Tensor t = new Tensor(0, 0, 0, Vector2.Zero);
        float dist = 0f;
        float d = 0f;
        Vector2 dif;

        for (int j = 0; j < weightedavgs.Count; ++j)
        {
            dif = new Vector2(weightedavgs[j].center2.X - pos.X, weightedavgs[j].center2.Y - pos.Y);
            dist += dif.Length();
        }

        for (int j = 0; j < weightedavgs.Count; ++j)
        {
            dif = new Vector2(weightedavgs[j].center2.X - pos.X, weightedavgs[j].center2.Y - pos.Y);
            d = dif.Length();
            if (weightedavgs.Count == 1) d = 0;

            if (weightedavgs[j].type == 0) t += ((dist - d) / dist) * weightedavgs[j];

            else if (weightedavgs[j].type == 1)
            {
                t += ((dist - d) / dist) * Tensor.FromXY(pos, weightedavgs[j].center2);
                //Vector2 v = Vector2.Zero;
                //Vector2 xy = pos - weightedavgs[j].center2;

                //if (x.A != 0 && x.B != 0)
                //{
                //    x.EigenVectors(out major, out minor);
                //    v = major;

                //    if (prev_dir == Vector2.Zero || Vector2.Dot(prev_dir, major) >= 0)
                //        t += ((dist - d) / dist) * x;
                //    else
                //        t += ((dist - d) / dist) * -x;
                //}
            }
        }
        return t;

    }
}

public class Edge
{
    public Vertex a;
    public Vertex b;

    public Vector2 direction;

    public Streamline streamline;

    public Edge(Streamline stream, Vertex v1, Vertex v2)
    {
        a = v1;
        b = v2;
        streamline = stream;
        direction = Vector2.Normalize(b.pos - a.pos);
    }

    public Edge MakeEdge(Vertex v1, Vertex v2, Streamline stream) {
        var e = new Edge(stream, v1, v2);

        v1.edges.Add(e);
        v2.edges.Add(e);

        return e;
    }
}

public class Vertex {
    public Vector2 pos;
    public List<Edge> edges = new List<Edge>();
    public Vertex(Vector2 p) => pos = p;
}

public class Seed: FastPriorityQueueNode
{
    public Vector2 pos;
    public Vector2 field;
    public Vector2 alt_field;

    public Seed(Vector2 start, Vector2 main, Vector2 alt)
    {
        pos = start;
        field = main;
        alt_field = alt;
    }

}

public class Streamline {
    public HashSet<Vertex> vertices = new HashSet<Vertex>();

    public Vertex first { get; private set; }
    public Vertex last { get; private set; }

    public Streamline(Vertex v1) {
        first = v1;
        last = v1;

        vertices.Add(v1);
    }
}