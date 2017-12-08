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
        return new Tensor(left.A + right.A, left.B + right.B, 2, Vector2.Zero);
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

    public static Tensor SampleDecayWeights(Vector2 pos, List<Tensor> weightedavgs) 
    {
        Tensor combined_tensor = new Tensor(0, 0, 0, Vector2.Zero);
        float d_squared = 0f;
        float decayConstant = -0.001f;

        foreach (var t in weightedavgs)
        {
            d_squared = (float)Math.Pow(t.center2.X - pos.X, 2) + (float)Math.Pow(t.center2.Y - pos.Y, 2);
            //Console.WriteLine("distance squared: " + d_squared);
            if (t.type == 0)
                combined_tensor += ((float)Math.Exp(decayConstant * d_squared) * t);

            else if (t.type == 1)
                combined_tensor += ((float)Math.Exp(decayConstant * d_squared) * Tensor.FromXY(pos, t.center2));
        }

        return combined_tensor;
    }

    public static Tensor Sample(Vector2 pos, List<Tensor> weightedavgs)
    {
        Tensor combined_tensor = new Tensor(0, 0, 0, Vector2.Zero);

        float total_weight = 0, d = 0;

        foreach (var t in weightedavgs) {
            total_weight += (t.center2-pos).Length();
        }

        foreach (var t in weightedavgs)
        {
            d = (t.center2 - pos).Length();
            if (weightedavgs.Count == 1) d = 0;

            if (t.type == 0) 
                combined_tensor += ((total_weight-d)/(total_weight) * t);

            else if (t.type == 1)
                combined_tensor += ((total_weight - d) / (total_weight) * Tensor.FromXY(pos, t.center2));
        }
        return combined_tensor;

    }
}

public class Edge
{
    public Vector2 a;
    public Vector2 b;

    public Vector2 direction;

    public Streamline streamline;

    public Edge(Streamline stream, Vector2 v1, Vector2 v2)
    {
        a = v1;
        b = v2;
        streamline = stream;
        direction = Vector2.Normalize(b - a);
    }
}

public class Seed: StablePriorityQueueNode
{
    public Vector2 pos;
    public bool tracingMajor;

    public Seed(Vector2 start, bool t)
    {
        pos = start;
        tracingMajor = t;
    }

}

public class Streamline {
    public HashSet<Vector2> vertices = new HashSet<Vector2>();

    public Vector2 first;
    public Vector2 last;

    public Streamline(Vector2 v1) {
        first = v1;
        last = v1;

        vertices.Add(v1);
    }

    public Edge Extend(Vector2 v) {
        vertices.Add(v);
        var temp = last;
        last = v;
        return new Edge(this, temp, last);
    }
}
