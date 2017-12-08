using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
//using Priority_Queue;

namespace roadmap
{
    public class RoadBuilder
    {
        public HashSet<Edge> all_edges;
        public List<Vector2> all_vertices;
        public List<Streamline> streams;
        public List<Tensor> tensors;
        public bool initialized;

        public PictureBox densityMap = new PictureBox();
        public PictureBox terrainMap = new PictureBox();
        public int width;
        public int height;

        public RoadBuilder()
        {
            streams = new List<Streamline>();
            all_edges = new HashSet<Edge>();
            tensors = new List<Tensor>();
            all_vertices = new List<Vector2>();
            initialized = false;

            terrainMap.Image = System.Drawing.Image.FromFile("terrain_map.png");
            terrainMap.Size = new Size(terrainMap.Image.Width, terrainMap.Image.Height);
        }

        public Color GetColor(int x, int y) 
        {
            float stretch_X = terrainMap.Width / (float)width;
            float stretch_Y = terrainMap.Height / (float)height;

            return ((Bitmap)terrainMap.Image).GetPixel((int)(stretch_X * x), (int)(stretch_Y * y));

        }

        public void setDimensions(int width, int height) {
            this.width = width;
            this.height = height;
        }

        public void Rk4_sample_field(out Vector2 major, out Vector2 minor, Vector2 point, Vector2 prev_dir, List<Tensor> w)
        {
            Vector2 k1_maj, k2_maj, k3_maj, k4_maj;
            Vector2 k1_min, k2_min, k3_min, k4_min;

            Tensor.Sample(point, w).EigenVectors(out major, out minor);
            k1_maj = corrected_vector(major, prev_dir);
            k1_min = corrected_vector(minor, prev_dir);

            Tensor.Sample(point + k1_maj / 2f, w).EigenVectors(out major, out minor);
            k2_maj = corrected_vector(major, prev_dir);
            Tensor.Sample(point + k1_min / 2f, w).EigenVectors(out major, out minor);
            k2_min = corrected_vector(minor, prev_dir);

            Tensor.Sample(point + k2_maj / 2f, w).EigenVectors(out major, out minor);
            k3_maj = corrected_vector(major, prev_dir);
            Tensor.Sample(point + k2_min / 2f, w).EigenVectors(out major, out minor);
            k3_min = corrected_vector(minor, prev_dir);

            Tensor.Sample(point + k3_maj, w).EigenVectors(out major, out minor);
            k4_maj = corrected_vector(major, prev_dir);
            Tensor.Sample(point + k3_min, w).EigenVectors(out major, out minor);
            k4_min = corrected_vector(minor, prev_dir);

            major = corrected_vector((k1_maj / 6f + k2_maj / 3f + k3_maj / 3f + k4_maj / 6f), prev_dir);
            minor = corrected_vector((k1_min / 6f + k2_min / 3f + k3_min / 3f + k4_min / 6f), prev_dir);
        }

        public void Rk4_sample_field_decay(out Vector2 major, out Vector2 minor, Vector2 point, Vector2 prev_dir, List<Tensor> w)
        {
            Vector2 k1_maj, k2_maj, k3_maj, k4_maj;
            Vector2 k1_min, k2_min, k3_min, k4_min;

            Tensor.SampleDecayWeights(point, w).EigenVectors(out major, out minor);
            k1_maj = corrected_vector(major, prev_dir);
            k1_min = corrected_vector(minor, prev_dir);

            Tensor.SampleDecayWeights(point + k1_maj / 2f, w).EigenVectors(out major, out minor);
            k2_maj = corrected_vector(major, prev_dir);
            Tensor.SampleDecayWeights(point + k1_min / 2f, w).EigenVectors(out major, out minor);
            k2_min = corrected_vector(minor, prev_dir);

            Tensor.SampleDecayWeights(point + k2_maj / 2f, w).EigenVectors(out major, out minor);
            k3_maj = corrected_vector(major, prev_dir);
            Tensor.SampleDecayWeights(point + k2_min / 2f, w).EigenVectors(out major, out minor);
            k3_min = corrected_vector(minor, prev_dir);

            Tensor.SampleDecayWeights(point + k3_maj, w).EigenVectors(out major, out minor);
            k4_maj = corrected_vector(major, prev_dir);
            Tensor.SampleDecayWeights(point + k3_min, w).EigenVectors(out major, out minor);
            k4_min = corrected_vector(minor, prev_dir);

            major = corrected_vector((k1_maj / 6f + k2_maj / 3f + k3_maj / 3f + k4_maj / 6f), prev_dir);
            minor = corrected_vector((k1_min / 6f + k2_min / 3f + k3_min / 3f + k4_min / 6f), prev_dir);
        }

        public Vector2 corrected_vector(Vector2 pos, Vector2 dir)
        {
            if (dir == Vector2.Zero || Vector2.Dot(dir, pos) >= 0)
                return pos;
            return -pos;
        }

        public void InitializeSeeds(Vector2 min, Vector2 max) 
        {
            if (initialized)
                return;

            initialized = true;

            //gridline tensors
            tensors.Add(Tensor.FromRTheta(20, 5 * Math.PI/4, new Vector2(30f, 50f)));
            tensors.Add(Tensor.FromRTheta(20, Math.PI, new Vector2(80f, 50f)));
            //tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(50f, 50f)));
            //tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(20f, 90f)));
            //tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(70f, 30f)));

            var diff = max - min;
            Vector2 major, minor;

            Rk4_sample_field(out major, out minor, min, Vector2.Zero, tensors);

            var seeds = RandomSeeds(min, max);
            SeedRunner(min, max, seeds, true, true);
        }

        private static IEnumerable<Seed> RandomSeeds(Vector2 min, Vector2 max)
        {
            var Dif = max - min;

            Random r = new Random();
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector2((float)r.Next((int)min.X, (int)max.X), (float)r.Next((int)min.Y, (int)max.Y)) + min;

                if (p.X < min.X || p.Y < min.Y || p.X > max.X || p.Y > max.Y)
                    i--;
                else
                    yield return new Seed(p, true);

            }
        }

        public void SeedRunner(Vector2 min, Vector2 max, IEnumerable<Seed> initialSeeds, bool forward, bool backward)
        {
            Queue seeds = new Queue();

            foreach (var initialSeed in initialSeeds)
                seeds.Enqueue(initialSeed);

            int i = 0;

            //Trace out roads for every single seed
            while (seeds.Count > 0)
            {
                Seed s = (Seed)seeds.Dequeue();

                if (forward)
                {
                    var stream = Trace(min, max, s, false, seeds, s.tracingMajor);
                    if (stream != null)
                    {
                        streams.Add(stream);
                    }

                    var stream2 = Trace(min, max, s, false, seeds, false);
                    if(stream2 != null) {
                        streams.Add(stream2);
                    }
                }

                if (backward)
                {
                    var stream = Trace(min, max, s, true, seeds, s.tracingMajor);
                    if (stream != null)
                    {
                        streams.Add(stream);
                        //streamCreated(stream);
                    }
                    var stream2 = Trace(min, max, s, true, seeds, false);
                    if (stream2 != null)
                    {
                        streams.Add(stream2);
                        //streamCreated(stream);
                    }
                }
                ++i;
            }
        }



        public Streamline Trace(Vector2 min, Vector2 max, Seed seed, bool reverse, Queue seeds, bool tracingMajor)
        {
            float maxSegmentLength = 0.5f;
            float mergeDistance = 1.5f;
            float cosineSearchAngle = 2;

            Streamline stream = new Streamline(seed.pos);

            var seedingDistance = float.MaxValue;
            var direction = Vector2.Zero;

            Vector2 position = seed.pos;
            all_vertices.Add(position);
            //Console.WriteLine("stream start" + seed.pos);

            Vector2 prev_direction = Vector2.Zero;
            Vector2 previous = prev_direction;

            //Extended naieve tracing (accumulate naieve traces, stop once we hit sample OR length limit)
            Vector2 d;
            for (var i = 0; i < 100; i++)
            {
                Vector2 major, minor;
                previous = prev_direction;
                float lengthSum = 0;
                for (var j = 0; j < 10 && lengthSum < maxSegmentLength; j++)
                {
                    Rk4_sample_field_decay(out major, out minor, position, previous, tensors);
                    if (tracingMajor)
                        d = major;
                    else
                        d = minor;
                    var l = d.Length();
                    lengthSum += d.Length();
                    direction += d;
                    previous = direction;

                    //escape if the curvature is too much
                    if (Vector2.Dot(prev_direction, d / l) < 0.9961f)
                        break;
                }

                if (i == 0 && reverse)
                    direction = -direction;

                //degenerate step check
                var segmentLength = direction.Length();
                if (segmentLength < 0.00005f)
                    break;

                //Excessive step check
                if (segmentLength > maxSegmentLength)
                {
                    //Console.WriteLine("here");
                    direction /= segmentLength * maxSegmentLength;
                    segmentLength = maxSegmentLength;
                }

                //Step along path
                //Vector2 temp = position;
                position += direction;
                seedingDistance += segmentLength;


                //Create the segment and break if it says so
                if (CreateAndCheckEdge(min, max, stream, position, direction, maxSegmentLength, mergeDistance, cosineSearchAngle))
                    break;

                //Accumulate seeds to trace into the alternative field
                //var seedSeparation = 50;
                //if (seedingDistance > seedSeparation)
                //{
                //    seedingDistance = 0;
                //    Seed s = new Seed(position, !seed.tracingMajor);
                //    seeds.Enqueue(s);
                //}

                prev_direction = direction;
            }

            return stream;
        }


        //This function was not created by us 
        public bool CreateAndCheckEdge(Vector2 min, Vector2 max, Streamline stream, Vector2 pos, Vector2 dir, float maxSegmentLength, float mergeDistance, float cosineSearchAngle)
        {
            bool stop_stream = false;

            //check if distance too short
            if (pos.X < min.X || pos.Y < min.Y || pos.X > max.X || pos.Y > max.Y)
            {
                stop_stream = true;
                if ((pos - stream.last).Length() < maxSegmentLength) 
                    return false;
            }

            //check if another vertex nearby to use instead
            var closestVertex = FindClosestVertex(pos, dir, mergeDistance, cosineSearchAngle, stream.last);
            //Console.WriteLine(closestVertex.X + " find closest vertex " + closestVertex.Y);

            //check if edge intersects with another edge
            Edge intersectedEdge = null;

            var intersectedPosition = FindEdgeIntersection(stream.last, pos, out intersectedEdge);

            if(intersectedEdge != null && closestVertex.Equals(Vector2.Zero)) {

                if ((intersectedPosition - intersectedEdge.a).Length() < mergeDistance)
                {
                    closestVertex = intersectedEdge.a;
                    //Console.WriteLine(closestVertex.X + " a - closest vertex " + closestVertex.Y);
                }
                else if ((intersectedPosition - intersectedEdge.b).Length() < mergeDistance) {
                    closestVertex = intersectedEdge.b;
                    //Console.WriteLine(closestVertex.X + " b - closest vertex " + closestVertex.Y);
                }
                else
                {
                    closestVertex = intersectedPosition;
                    all_edges.Remove(intersectedEdge);
                    all_edges.Add(new Edge(intersectedEdge.streamline, intersectedEdge.a, intersectedPosition));
                    all_edges.Add(new Edge(intersectedEdge.streamline, intersectedPosition, intersectedEdge.b));
                    intersectedEdge.streamline.vertices.Add(intersectedPosition);
                }
            }

            //check and handle intersection
            //Console.Write(stop_stream);

            if (!closestVertex.Equals(Vector2.Zero)) stop_stream = true;
            else closestVertex = pos;

            //Console.Write(stop_stream);

            //if new vertex is being added to the streamline, or if we're making a loop
            if (!stream.vertices.Contains(closestVertex) || stream.first.Equals(closestVertex))
            {
                //Edge e = stream.Extend(closestVertex);
                stream.vertices.Add(closestVertex);
                Edge e = new Edge(stream, stream.last, closestVertex);
                stream.last = closestVertex;

                foreach(var s in streams)
                {
                    if( all_edges.Contains(new Edge(s, stream.last, closestVertex)) || 
                        all_edges.Contains(new Edge(s, closestVertex, stream.last)) )
                    {
                        e = null;
                        break;
                    }
                }

                if (e == null || stream.first.Equals(closestVertex)) stop_stream = true;
                all_edges.Add(e);

            }
            //Console.Write(stop_stream + "\n");
            //Console.WriteLine(closestVertex);
            return stop_stream;
        }

        //checks if edge intersects with any other edge, and returns the first intersecting position
        //https://stackoverflow.com/questions/563198/whats-the-most-efficent-way-to-calculate-where-two-line-segments-intersect
        public Vector2 FindEdgeIntersection(Vector2 v1, Vector2 v2, out Edge intersected) {
            
            intersected = null;
            float best_t = 99999f, s, t;

            Vector2 slope = v2 - v1;
            Vector2 slope2;
            Vector2 intersectedPosition = Vector2.Zero;

            foreach (Edge e in all_edges)
            {
                slope2 = e.b - e.a;

                s = (-slope.Y * (v1.X - e.a.X) + slope.X * (v1.Y - e.a.Y)) / (-slope2.X * slope.Y + slope.X * slope2.Y);
                t = (slope2.X * (v1.Y - e.a.Y) - slope2.Y * (v1.X - e.a.X)) / (-slope2.X * slope.Y + slope.X * slope2.Y);

                if (s > 0 && s < 1 && t >= 0 && t <= 1 && t < best_t)
                {
                    intersected = e;
                    intersectedPosition = new Vector2(v1.X + (t * slope.X), v1.Y + (t * slope.Y));
                    best_t = t;
                }

            }
            return intersectedPosition; 

        }

        /*Parameters
         * pos : position streamline is tracing through
         * dir : direction of streamline
         * mergeDistance: max search distance from pos
         * angle: max angle of merge allowed
         * 
         * Returns: the vertex closest to pos
         */
        public Vector2 FindClosestVertex(Vector2 pos, Vector2 dir, float mergeDistance, float angle, Vector2 last)
        {
            float closestDistance = mergeDistance;
            Vector2 closeEnoughVertex = Vector2.Zero;

            foreach (var vertex in all_vertices)
            {
                if (vertex.Equals(last) || vertex.Equals(pos)) continue;

                var diff = vertex - pos;
                var l = diff.Length();

                if (l > closestDistance || l < 0.00005f) continue;

                if (dir != Vector2.Zero && Vector2.Dot(diff / l, dir) < angle) continue;

                closestDistance = l;
                closeEnoughVertex = vertex;
            }

            return closeEnoughVertex;
        }
    }
}