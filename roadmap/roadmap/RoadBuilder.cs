using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using CSharp.DataStructures.QuadTreeSpace;
//using Priority_Queue;

namespace roadmap
{
    public class RoadBuilder
    {
        public QuadTree<Vector2> s;
        public HashSet<Edge> all_edges;
        public List<Vector2> all_vertices;
        public List<Streamline> streams;
        public List<Tensor> tensors;
        public bool initialized;
        public Tuple<Vector2, Vector2>[,] eigen_cache;

        public PictureBox densityMap = new PictureBox();
        public PictureBox terrainMap = new PictureBox();
        public Bitmap terrain;
        public int width;
        public int height;

        public List<Tensor> polyline_river;
        public HashSet<Edge> test_river;
        public RoadBuilder()
        {
            streams = new List<Streamline>();
            all_edges = new HashSet<Edge>();
            tensors = new List<Tensor>();
            all_vertices = new List<Vector2>();
            polyline_river = new List<Tensor>();
            test_river = new HashSet<Edge>();
            eigen_cache = new Tuple<Vector2, Vector2>[800, 800];
            for (int i = 0; i < 800; i++)
                for (int j = 0; j < 800; j++)
                    eigen_cache[i, j] = new Tuple<Vector2, Vector2>(Vector2.Zero, Vector2.Zero);
            initialized = false;
            terrain = new Bitmap(Image.FromFile("terrain_map.png"), 800, 800);
            terrainMap.Image = terrain;
            terrainMap.Size = new Size(800, 800);

            //gridline tensors
            //tensors.Add(Tensor.FromRTheta(20, 5 * Math.PI / 4, new Vector2(200f, 50f)));
            tensors.Add(Tensor.FromRTheta(20, Math.PI, new Vector2(400f, 450f)));
            //tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(250f, 250f)));
            //tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(100f, 90f)));
            tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(370f, 430f)));
        }

        public Color GetColor(int x, int y) 
        {
            //float stretch_X = terrainMap.Width / (float)width;
            //float stretch_Y = terrainMap.Height / (float)height;
            //Console.WriteLine(x + " "+ y);
            //Bitmap b = new Bitmap(terrainMap.Image, 800, 800);
            return terrain.GetPixel((int)( x), (int)( y));

        }

        public void setDimensions(int width, int height)
        {
            bool recalc = false;
            if (width != this.width || height != this.height)
                recalc = true;

            this.width = width;
            this.height = height;

            if (recalc)
            {
                //generate_poly_lines();
                recalc = false;
            }
        }

        public void generate_poly_lines() {
            polyline_river.Clear();
            test_river.Clear();
            int x_corner_1 = 0;
            int y_corner_1 = 0;

            bool check1 = false;
            for (int i = 1; i < width; ++i)
            {
                for (int j = 1; j < height; ++j)
                {
                    if (GetColor(i, j).R == 0)
                    {
                        x_corner_1 = i;
                        y_corner_1 = j - 1;
                        check1 = true;
                        break;
                    }
                }

                if (check1) break;
            }

            int x_corner_2 = 0;
            int y_corner_2 = 0;

            bool check2 = false;
            for (int i = width - 2; i >= 0; i--) 
            {
                for (int j = height - 2; j >= 0; j--) 
                {
                    if (GetColor(i, j).R == 0)
                    {
                        x_corner_2 = i;
                        y_corner_2 = j + 1;
                        check2 = true;
                        break;
                    }
                }

                if (check2) break;
            }

            Vector2 corner1 = new Vector2(x_corner_1, y_corner_1);
            Vector2 corner2 = new Vector2(x_corner_2, y_corner_2);

            traverse_river(corner1, true, true);
            traverse_river(corner1, false, true);
            traverse_river(corner2, true, false);
            traverse_river(corner2, false, false);

            HashSet<Edge> redundant = new HashSet<Edge>();
            foreach (var e in test_river) 
            {
                foreach (var f in test_river)
                {
                    if (e != f && e.b == f.a)
                    {
                        if ( (Vector2.Normalize(e.b - e.a) - Vector2.Normalize(f.b - f.a)).Length() < 0.0005f )
                        {
                            e.b = f.b;
                            redundant.Add(f);
                        }
                    }
                }
            }

            foreach (var e in redundant) {
                test_river.Remove(e);
            }

            foreach (var e in test_river) {
                Vector2 dif = new Vector2(e.b.X - e.a.X, e.b.Y - e.a.Y);
                double theta = getAngle(dif.X, dif.Y);
                polyline_river.Add(Tensor.FromRTheta(dif.Length(), theta, e.a));
            }
        }

        public void traverse_river(Vector2 corner, bool up, bool over) {
            
            if(corner.X < 2 || corner.X > width - 2 || corner.Y < 2 || corner.Y > height - 2) {
                return;
            }

            int dir_x, dir_y;
            check_surroundings(out dir_x, out dir_y, corner, up, over);
            Vector2 next = new Vector2(corner.X + dir_x, corner.Y + dir_y);

            test_river.Add(new Edge(corner, next));
            traverse_river(next, up, over);
        }

        public double getAngle(float delta_x, float delta_y) 
        {
            if(delta_x > 0) {
                if(delta_y < 0) 
                {
                    return 7 * Math.PI / 4;    
                }
                else if (delta_y > 0)
                {
                    return Math.PI / 4;
                }
                else {
                    return 0;
                }
            }
            else if(delta_x < 0) {
                if (delta_y < 0)
                {
                    return 5 * Math.PI / 4;
                }
                else if (delta_y > 0)
                {
                    return 3 * Math.PI / 4;
                }
                else
                {
                    return Math.PI;
                }
            }
            else {
                if (delta_y < 0)
                {
                    return Math.PI / 2;
                }
                else if (delta_y > 0)
                {
                    return 3 * Math.PI / 2;
                }
                else
                {
                    return 0;
                }
            }
        }
        public void check_surroundings(out int dir_x, out int dir_y, Vector2 corner, bool up, bool over) {
            bool r = false, r_up = false, r_down = false;

            if (GetColor((int)corner.X + 1, (int)corner.Y).R > 0) r = true;
            if (GetColor((int)corner.X + 1, (int)corner.Y - 1).R > 0) r_up = true;
            if (GetColor((int)corner.X + 1, (int)corner.Y + 1).R > 0) r_down = true;

            if(!r && !r_up && !r_down) {
                dir_x = 0;
                dir_y = -1;
            }
            else if(r) {
                dir_x = 1;
                dir_y = 0;
            }
            else if(r_up) {
                dir_x = 1;
                dir_y = -1;
            }
            else {
                dir_x = 1;
                dir_y = 1;
            }

            if(!up) {
                bool d = false, d_l = false, l = false;
                if (GetColor((int)corner.X, (int)corner.Y + 1).R > 0) d = true;
                if (GetColor((int)corner.X - 1, (int)corner.Y + 1).R > 0) d_l = true;
                if (GetColor((int)corner.X - 1, (int)corner.Y).R > 0) l = true;

                if(over) {
                    if (d)
                    {
                        dir_x = 0;
                        dir_y = 1;
                    }
                    else if (d_l)
                    {
                        dir_x = -1;
                        dir_y = 1;
                    }
                    else
                    {
                        dir_x = -1;
                        dir_y = 0;
                    }
                }
                else {
                    if (l)
                    {
                        dir_x = -1;
                        dir_y = 0;
                    }
                    else if (d_l)
                    {
                        dir_x = -1;
                        dir_y = 1;
                    }
                    else
                    {
                        dir_x = 0;
                        dir_y = 1;
                    }
                }
            }
        }
        public void Rk4_sample_field(out Vector2 major, out Vector2 minor, Vector2 point, Vector2 prev_dir, List<Tensor> w)
        {
            //w.AddRange(polyline_river);

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
            //w.AddRange(polyline_river);

            Vector2 k1_maj, k2_maj, k3_maj, k4_maj;
            Vector2 k1_min, k2_min, k3_min, k4_min;
            var seg_pos = point;
            if (seg_pos.X >= 0 && seg_pos.X < 800 && seg_pos.Y >= 0 && seg_pos.Y < 800)
            {
                var cache_element = eigen_cache[(int)(seg_pos).X, (int)(seg_pos).Y];
                if (cache_element.Equals(new Tuple<Vector2, Vector2>(Vector2.Zero, Vector2.Zero)))
                {
                    Tensor.SampleDecayWeights(point, w).EigenVectors(out major, out minor); eigen_cache[(int)seg_pos.X, (int)seg_pos.Y] = new Tuple<Vector2, Vector2>(major, minor);
                    eigen_cache[(int)(seg_pos).X, (int)(seg_pos).Y] = new Tuple<Vector2, Vector2>(major, minor);
                }

                else
                {
                    major = cache_element.Item1;
                    minor = cache_element.Item2;
                }
            }
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
            Console.WriteLine("Starting timer");
            Console.WriteLine(min + " " + max);
            Stopwatch timer = new Stopwatch();
            timer.Start();

            if (initialized)
                return;

            initialized = true;

            var diff = max - min;
            Vector2 major, minor;

            Rk4_sample_field(out major, out minor, min, Vector2.Zero, tensors);

            var seeds = RandomSeeds(min, max);
            SeedRunner(min, max, seeds, true, true);
            timer.Stop();
            Console.WriteLine("Done! Time elapsed: " + timer.Elapsed);
        }

        private static IEnumerable<Seed> RandomSeeds(Vector2 min, Vector2 max)
        {
            var Dif = max - min;

            Random r = new Random(5);
            for (int i = 0; i < 5; i++)
            {
                var p = new Vector2(r.Next((int)min.X, (int)max.X), r.Next((int)min.Y, (int)max.Y)) + min;

                if (p.X < min.X || p.Y < min.Y || p.X > max.X || p.Y > max.Y)
                    i--;
                else
                    yield return new Seed(p, true);
            }
        }

        public void SeedRunner(Vector2 min, Vector2 max, IEnumerable<Seed> initialSeeds, bool forward, bool backward)
        {
            Console.Write(terrainMap.Width + " X " + terrainMap.Height);
            Queue seeds = new Queue();

            foreach (var initialSeed in initialSeeds)
                seeds.Enqueue(initialSeed);

            int i = 0;

            //Trace out roads for every single seed
            while (seeds.Count > 0)
            {
                Seed s = (Seed)seeds.Dequeue();
                Console.WriteLine(seeds.Count + " " + s.pos);
                if (forward)
                {
                    var stream = Trace(min, max, s, false, seeds, s.tracingMajor);
                    if (stream != null)
                    {
                        streams.Add(stream);
                    }

                    //var stream2 = Trace(min, max, s, false, seeds, false);
                    //if(stream2 != null) {
                    //    streams.Add(stream2);
                    //}
                }

                if (backward)
                {
                    var stream = Trace(min, max, s, true, seeds, s.tracingMajor);
                    if (stream != null)
                    {
                        streams.Add(stream);
                        //streamCreated(stream);
                    }
                    //var stream2 = Trace(min, max, s, true, seeds, false);
                    //if (stream2 != null)
                    //{
                    //    streams.Add(stream2);
                    //    //streamCreated(stream);
                    //}
                }
                ++i;
            }
        }



        public Streamline Trace(Vector2 min, Vector2 max, Seed seed, bool reverse, Queue seeds, bool tracingMajor)
        {
            int maxSegmentLength = 20;
            int mergeDistance = 30;
            float cosineSearchAngle = 0.15f;

            var ss = FindClosestVertex(seed.pos, Vector2.Zero, mergeDistance, cosineSearchAngle, Vector2.Zero);
            Streamline stream;
            //stream = new Streamline(seed.pos);

            if (ss.Equals(Vector2.Zero))
                stream = new Streamline(seed.pos);
            else
            {
                //stream = new Streamline(ss);
                //all_edges.Add(new Edge(null, seed.pos, ss));
                return null;
            }


            var seedingDistance = float.MaxValue;
            var segment = Vector2.Zero;

            Vector2 position = seed.pos;
            all_vertices.Add(position);

            Vector2 prev_direction = Vector2.Zero;
            Vector2 p;

            //Extended naieve tracing (accumulate naieve traces, stop once we hit sample OR length limit)
            Vector2 temp;
            for (var i = 0; i < 500; i++)
            {
                Vector2 major, minor;
                p = prev_direction;

                //Rk4_sample_field(out major, out minor, position, previous, tensors);

                //if (tracingMajor)
                //    direction = major;
                //else
                //direction = minor;
                var segmentLength = 0.0f;
                segment = Vector2.Zero;
                for (var j = 0; j < 20 && segmentLength < maxSegmentLength; j++)
                {

                    var seg_pos = position + segment;
                    if (seg_pos.X >= min.X && seg_pos.X < max.X && seg_pos.Y >= min.Y && seg_pos.Y < max.Y)
                    {
                        var cache_element = eigen_cache[(int)(seg_pos).X, (int)(seg_pos).Y];
                        if (cache_element.Equals(new Tuple<Vector2, Vector2>(Vector2.Zero, Vector2.Zero)))
                        {
                            Rk4_sample_field(out major, out minor, seg_pos, Vector2.Normalize(p), tensors);
                            eigen_cache[(int)seg_pos.X, (int)seg_pos.Y] = new Tuple<Vector2, Vector2>(major, minor);
                            if (tracingMajor)
                                temp = major;
                            else
                                temp = minor;
                        }

                        else
                        {
                            if (tracingMajor)
                                temp = cache_element.Item1;
                            else
                                temp = cache_element.Item2;
                        }
                        segment += temp;
                        segmentLength = segment.Length();
                        p = temp;
                        if (Vector2.Dot(Vector2.Normalize(prev_direction), temp / temp.Length()) < 0.9961f)
                            break;
                    }
                    else break;

                }
                if (i == 0 && reverse)
                    segment = -segment;

                //degenerate step check
                if (segmentLength < 0.000005f)
                {
                    Console.WriteLine("gets here1");
                    break;
                }

                //Excessive step check
                if (segmentLength > maxSegmentLength)
                {
                    segment = segment / segmentLength * maxSegmentLength;
                    segmentLength = maxSegmentLength;
                }
                if ((int)position.X < 780 ){
                    if (GetColor((int)position.X + 20, (int)position.Y).G == 0)
                    {
                        Console.WriteLine("gets here coloor");

                        break;
                    }
                }
                else if (GetColor((int)position.X, (int)position.Y).G == 0)
                {
                    Console.WriteLine("gets here coloor");

                    break;
                }


                //Step along path
                //Vector2 temp = position;
                position += segment;
                seedingDistance += segmentLength;

                //Create the segment and break if it says so
                if (CreateAndCheckEdge(min, max, stream, position, Vector2.Normalize(segment), maxSegmentLength, mergeDistance, cosineSearchAngle)) {
                    Console.WriteLine("gets here2");
                    break;
                }
    

                //Accumulate seeds to trace into the alternative field
                var seedSeparation = 100;
                if (seedingDistance > seedSeparation)
                {
                    seedingDistance = 0;
                    seeds.Enqueue(new Seed(position, !seed.tracingMajor));
                }

                prev_direction = segment;
            }
            Console.WriteLine(position);
            return stream;
        }


        //This function was not created by us 
        public bool CreateAndCheckEdge(Vector2 min, Vector2 max, Streamline stream, Vector2 pos, Vector2 dir, float maxSegmentLength, float mergeDistance, float cosineSearchAngle)
        {
            bool stop_stream = false;

            //check if distance too short
            if (pos.X < min.X || pos.Y < min.Y || pos.X > max.X || pos.Y > max.Y)
            {
                Console.Write("gets here2.1");
                Console.WriteLine(pos);


                stop_stream = true;
                //if ((pos - stream.last).Length() < maxSegmentLength) 
                    //return false;
            }

            //check if another vertex nearby to use instead
            var closestVertex = FindClosestVertex(pos, Vector2.Normalize(dir), mergeDistance, cosineSearchAngle, stream.last);
            //Console.WriteLine(closestVertex.X + " find closest vertex " + closestVertex.Y);

            //check if edge intersects with another edge
            Edge intersectedEdge = null;

            var intersectedPosition = FindEdgeIntersection(stream.last, pos, out intersectedEdge);

            if(intersectedEdge != null && closestVertex.Equals(Vector2.Zero)) {

                if ((intersectedPosition - intersectedEdge.a).Length() < 5)
                {
                    closestVertex = intersectedEdge.a;
                }
                else if ((intersectedPosition - intersectedEdge.b).Length() < 5) {
                    closestVertex = intersectedEdge.b;
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

            if (!closestVertex.Equals(Vector2.Zero))
            {
                stop_stream = true;
            }
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
                        return true;
                    }
                }
                all_edges.Add(e);

            }
            if (stream.first.Equals(stream.last))
            {
                stop_stream = true;

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
                if (last != Vector2.Zero && (vertex.Equals(last) || vertex.Equals(pos))) continue;

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