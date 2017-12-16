using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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
        public Tuple<Vector2, Vector2>[,] eigen_cache;
        public Queue seeds;

        public PictureBox densityMap = new PictureBox();
        public PictureBox terrainMap = new PictureBox();
        public Bitmap terrain;
        public Bitmap density;
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
            density = new Bitmap(Image.FromFile("density_map.png"), 800, 800);

            //gridline tensors
            //tensors.Add(Tensor.FromRTheta(1.0, Math.PI, new Vector2(400f, 400f)));
            tensors.Add(Tensor.FromRTheta(1.0, Math.PI, new Vector2(600f, 600f)));

            tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(150f, 150f)));

            tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(650f, 600f)));

            seeds = new Queue();
        }

        public Color GetColor(int x, int y) 
        {
            return terrain.GetPixel((int)( x), (int)( y));

        }


        public Color GetDensity(int x, int y)
        {
            return density.GetPixel((int)(x), (int)(y));

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
            Vector2 k1_maj, k2_maj, k3_maj, k4_maj;
            Vector2 k1_min, k2_min, k3_min, k4_min;

            Tensor.Sample(point, w).EigenVectors(out major, out minor);
            k1_maj = fix_direction(major, prev_dir);
            k1_min = fix_direction(minor, prev_dir);

            Tensor.Sample(point + k1_maj / 2f, w).EigenVectors(out major, out minor);
            k2_maj = fix_direction(major, prev_dir);
            Tensor.Sample(point + k1_min / 2f, w).EigenVectors(out major, out minor);
            k2_min = fix_direction(minor, prev_dir);

            Tensor.Sample(point + k2_maj / 2f, w).EigenVectors(out major, out minor);
            k3_maj = fix_direction(major, prev_dir);
            Tensor.Sample(point + k2_min / 2f, w).EigenVectors(out major, out minor);
            k3_min = fix_direction(minor, prev_dir);

            Tensor.Sample(point + k3_maj, w).EigenVectors(out major, out minor);
            k4_maj = fix_direction(major, prev_dir);
            Tensor.Sample(point + k3_min, w).EigenVectors(out major, out minor);
            k4_min = fix_direction(minor, prev_dir);

            major = fix_direction((k1_maj / 6f + k2_maj / 3f + k3_maj / 3f + k4_maj / 6f), prev_dir);
            minor = fix_direction((k1_min / 6f + k2_min / 3f + k3_min / 3f + k4_min / 6f), prev_dir);
        }

        //NOT USED
        public void Rk4_sample_field_decay(out Vector2 major, out Vector2 minor, Vector2 point, Vector2 prev_dir, List<Tensor> w)
        {
            Vector2 k1_maj, k2_maj, k3_maj, k4_maj;
            Vector2 k1_min, k2_min, k3_min, k4_min;
           
            Tensor.SampleDecayWeights(point, w).EigenVectors(out major, out minor);
            k1_maj = fix_direction(major, prev_dir);
            k1_min = fix_direction(minor, prev_dir);

            Tensor.SampleDecayWeights(point + k1_maj / 2f, w).EigenVectors(out major, out minor);
            k2_maj = fix_direction(major, prev_dir);
            Tensor.SampleDecayWeights(point + k1_min / 2f, w).EigenVectors(out major, out minor);
            k2_min = fix_direction(minor, prev_dir);

            Tensor.SampleDecayWeights(point + k2_maj / 2f, w).EigenVectors(out major, out minor);
            k3_maj = fix_direction(major, prev_dir);
            Tensor.SampleDecayWeights(point + k2_min / 2f, w).EigenVectors(out major, out minor);
            k3_min = fix_direction(minor, prev_dir);

            Tensor.SampleDecayWeights(point + k3_maj, w).EigenVectors(out major, out minor);
            k4_maj = fix_direction(major, prev_dir);
            Tensor.SampleDecayWeights(point + k3_min, w).EigenVectors(out major, out minor);
            k4_min = fix_direction(minor, prev_dir);

            major = fix_direction((k1_maj / 6f + k2_maj / 3f + k3_maj / 3f + k4_maj / 6f), prev_dir);
            minor = fix_direction((k1_min / 6f + k2_min / 3f + k3_min / 3f + k4_min / 6f), prev_dir);
        }

        public Vector2 fix_direction(Vector2 pos, Vector2 dir)
        {
            if (dir == Vector2.Zero || Vector2.Dot(dir, pos) >= 0)
                return pos;
            return -pos;
        }

        /*Parameters
         * min : bounds
         * max : bounds
         * 
         * Creates entire roadmap
         */
        public void Create(Vector2 min, Vector2 max) 
        {
            Console.WriteLine("Starting timer");
            Stopwatch timer = new Stopwatch();
            timer.Start();

            if (initialized)
                return;

            initialized = true;

            var diff = max - min;
            MakeInitialSeeds(min, max);
            SeedRunner(min, max);
            timer.Stop();
            Console.WriteLine("Done! Time elapsed: " + timer.Elapsed);
        }

        /*Parameters
         * min : bounds
         * max : bounds
         * 
         * Populates queue with 10 random seeds within the bounds of the box
         */
        public void MakeInitialSeeds(Vector2 min, Vector2 max)
        {
            var Dif = max - min;

            Random r = new Random(5);
            for (int i = 0; i < 10; i++)
            {
                var p = new Vector2(r.Next((int)min.X, (int)max.X), r.Next((int)min.Y, (int)max.Y)) + min;

                if (p.X < min.X || p.Y < min.Y || p.X > max.X || p.Y > max.Y)
                    i--;
                else
                    seeds.Enqueue(new Seed(p, true));
            }
        }

        /*Parameters
         * min : bounds
         * max : bounds
         * 
         * Calls trace twice for every seed.
         */
        public void SeedRunner(Vector2 min, Vector2 max)
        {
            while (seeds.Count > 0)
            {
                Seed s = (Seed)seeds.Dequeue();

                var stream = Trace(min, max, s, false, s.tracingMajor);
                if (stream != null) streams.Add(stream);

                stream = Trace(min, max, s, true, s.tracingMajor);
                if (stream != null) streams.Add(stream);
            }
        }


        /*Parameters
         * min : bounds
         * max : bounds
         * seed: start point of streamline
         * reverse: streamline goes in reverse for one segment to start seed 
         *          generation in the opposite direction as well
         * tracingMajor: tells us which eigen vector to trace along (major or minor)
         * 
         * Returns: the full traced streamline
         */
        public Streamline Trace(Vector2 min, Vector2 max, Seed seed, bool reverse, bool tracingMajor)
        {
            int maxSegmentLength = 5;
            int mergeDistance = 12;

            var ss = FindClosestVertex(seed.pos, Vector2.Zero, mergeDistance, seed.pos);
            Streamline stream = null;

            if (ss.Equals(Vector2.Zero))
                stream = new Streamline(seed.pos);
           
            else
            {
                stream = new Streamline(ss);
                stream.last = seed.pos;
                all_edges.Add(new Edge(stream, seed.pos, ss));
                return stream;
            }

            //start seed step distance high, 
            //so seed is created after first segment is drawn
            var seedStepDistance = float.MaxValue;
            var segment = Vector2.Zero;

            Vector2 position = seed.pos;
            all_vertices.Add(position);

            Vector2 prev_direction = Vector2.Zero;
            Vector2 p;

            Vector2 temp;

            //continues tracing stream until stream breaks;
            for (var i = 0; i < 500; i++)
            {
                Vector2 major, minor;
                p = prev_direction;

                var segmentLength = 0.0f;
                segment = Vector2.Zero;

                //creates segment
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
                        if (Vector2.Dot(prev_direction, segment / segment.Length()) < .9f)
                            break;
                    }
                    else break;

                }
                if (i == 0 && reverse) segment = -segment;

                //on a tensor degenerate point
                if (segmentLength < 0.00005f) break;

                //the offset of x is a temporary fix to line up roads to stop on river
                //Color check
                if ((int)position.X < 780 && GetColor((int)position.X + 20, (int)position.Y).G == 0) break;
                if (GetColor((int)position.X, (int)position.Y).G == 0) break;


                //Step along path
                position += segment;
                seedStepDistance += segmentLength;

                //Create the segment and break if it says so
                if (MakeEdgeAndStopStream(min, max, stream, position, Vector2.Normalize(segment), mergeDistance)) break;


                //Get seed density from density map
                Color dense = GetDensity((int)position.X, (int)position.Y);
                double yo = dense.GetBrightness();
                var density = 10 + 100 * (1 - yo);

                //adds new seed that is traced along the opposite eigenvector
                if (seedStepDistance > density)
                {
                    seedStepDistance = 0;
                    seeds.Enqueue(new Seed(position, !seed.tracingMajor));
                }

                prev_direction = Vector2.Normalize(segment);
            }
            return stream;
        }


        /*Parameters
         * min : bounds
         * max : bounds
         * stream: streamline that we're tracing and adding an edge to
         * pos: vertex being added to stream
         * dir: direction of edge about to be created
         * mergedistance: how far to search for neighbor vertices
         * 
         * Returns: a boolean stating whether or not to stop the stream
         */
        public bool MakeEdgeAndStopStream(Vector2 min, Vector2 max, Streamline stream, Vector2 pos, Vector2 dir, float mergeDistance)
        {
            bool stop_stream = false;

            //check if distance too short
            if (pos.X < min.X || pos.Y < min.Y || pos.X > max.X || pos.Y > max.Y)
                stop_stream = true;

            //check if another vertex nearby to use instead
            var closestVertex = FindClosestVertex(pos, Vector2.Normalize(dir), mergeDistance, stream.last);

            //check if edge intersects with another edge
            Edge intersectedEdge = null;

            var intersectedPosition = FindEdgeIntersection(stream.last, pos, out intersectedEdge);

            if (intersectedEdge != null && closestVertex.Equals(Vector2.Zero)) 
                closestVertex = intersectedPosition;

            //check and handle intersection
            if (!closestVertex.Equals(Vector2.Zero))
                stop_stream = true;
            else closestVertex = pos;

            //if new vertex is being added to the streamline, or if we're making a loop
            if (!stream.vertices.Contains(closestVertex) || stream.first.Equals(closestVertex))
            {
                stream.vertices.Add(closestVertex);
                Edge e = new Edge(stream, stream.last, closestVertex);
                stream.last = closestVertex;
                all_edges.Add(e);

            }
            if (stream.first.Equals(stream.last))
                stop_stream = true;
            return stop_stream;
        }

        //checks if edge intersects with any other edge, and returns the first intersecting position
        //https://stackoverflow.com/questions/563198/whats-the-most-efficent-way-to-calculate-where-two-line-segments-intersect

        /*Parameters
         * v1 : last vertex on stream
         * v2 : new verterx to add to stream
         * output intersected edge
         * 
         * Returns: a pos where the new edge intersects with an old edge
         */
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
        public Vector2 FindClosestVertex(Vector2 pos, Vector2 dir, float mergeDistance, Vector2 last)
        {
            float closestDistance = mergeDistance;
            Vector2 closeEnoughVertex = Vector2.Zero;

            foreach (var vertex in all_vertices)
            {
                if ((vertex.Equals(last) || vertex.Equals(pos))) continue;

                var diff = vertex - pos;
                var l = diff.Length();

                if (l > closestDistance) continue;

                if (dir != Vector2.Zero && Vector2.Dot(diff / l, dir) < 0.15f) continue;

                closestDistance = l;
                closeEnoughVertex = vertex;
            }

            return closeEnoughVertex;
        }
    }
}