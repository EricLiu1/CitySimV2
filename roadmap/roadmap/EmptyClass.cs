﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Priority_Queue;

namespace roadmap
{
    public class RoadBuilder
    {
        public static List<Edge> all_edges;
        public static List<Vertex> all_vertices;
        public HashSet<Streamline> streams;
        public List<Tensor> tensors;

        public RoadBuilder()
        {
            streams = new HashSet<Streamline>();
            all_edges = new List<Edge>();
            all_vertices = new List<Vertex>();
        }

        public void Rk4_sample_field(out Vector2 major, out Vector2 minor, Vector2 point, Vector2 prev_dir, List<Tensor> w, bool tracingMajor)
        {
            Vector2 k1_maj, k2_maj, k3_maj, k4_maj;
            Vector2 k1_min, k2_min, k3_min, k4_min;

            Tensor.Sample(point, prev_dir, w, tracingMajor).EigenVectors(out major, out minor);
            k1_maj = corrected_vector(major / 10f, prev_dir);
            k1_min = corrected_vector(minor / 100f, prev_dir);

            Tensor.Sample(point + k1_maj / 2f, prev_dir, w, tracingMajor).EigenVectors(out major, out minor);
            k2_maj = corrected_vector(major / 10f, prev_dir);
            Tensor.Sample(point + k1_min / 2f, prev_dir, w, tracingMajor).EigenVectors(out major, out minor);
            k2_min = corrected_vector(minor / 100f, prev_dir);

            Tensor.Sample(point + k2_maj / 2f, prev_dir, w, tracingMajor).EigenVectors(out major, out minor);
            k3_maj = corrected_vector(major / 10f, prev_dir);
            Tensor.Sample(point + k2_min / 2f, prev_dir, w, tracingMajor).EigenVectors(out major, out minor);
            k3_min = corrected_vector(minor / 100f, prev_dir);

            Tensor.Sample(point + k3_maj, prev_dir, w, tracingMajor).EigenVectors(out major, out minor);
            k4_maj = corrected_vector(major / 10f, prev_dir);
            Tensor.Sample(point + k3_min, prev_dir, w, tracingMajor).EigenVectors(out major, out minor);
            k4_min = corrected_vector(minor / 100f, prev_dir);

            major = corrected_vector(Vector2.Normalize(10f * (k1_maj / 6f + k2_maj / 3f + k3_maj / 3f + k4_maj / 6f)), prev_dir);
            minor = corrected_vector(Vector2.Normalize(10f * (k1_min / 6f + k2_min / 3f + k3_min / 3f + k4_min / 6f)), prev_dir);

            //return corrected_vector(Vector2.Normalize(10f * (k1 / 6f + k2 / 3f + k3 / 3f + k4 / 6f)), prev_dir);

        }

        public Vector2 corrected_vector(Vector2 pos, Vector2 dir)
        {
            if (dir == Vector2.Zero || Vector2.Dot(dir, pos) >= 0)
                return pos;
            return -pos;
        }

        public void InitializeSeeds(Vector2 min, Vector2 max) 
        {
            //gridline tensors
            tensors.Add(Tensor.FromRTheta(2, 3.1415926, new Vector2(0.3f, 0.5f)));
            tensors.Add(Tensor.FromRTheta(0.5, 3.1415926, new Vector2(0.8f, 0.5f)));
            tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(0.5f, 0.5f)));
            tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(0.2f, 0.9f)));
            tensors.Add(Tensor.FromXY(new Vector2(0, 0), new Vector2(0.7f, 0.3f)));

            var diff = max - min;
            Vector2 major, minor;

            Rk4_sample_field(out major, out minor, min, Vector2.Zero, tensors, true);

            var seeds = RandomSeeds(major, minor, min, max);
            SeedRunner(seeds, true, true);
        }

        private static IEnumerable<Seed> RandomSeeds(Vector2 major, Vector2 minor, Vector2 min, Vector2 max)
        {
            var Dif = max - min;

            Random r = new Random();
            for (int i = 0; i < 10; i++)
            {
                var p = new Vector2((float)r.NextDouble(), (float)r.NextDouble()) + min;

                if (p.X < 0 || p.Y < 0 || p.X > 1 || p.Y > 1)
                    i--;
                else
                    yield return new Seed(p, major, minor);

            }
        }

        public void SeedRunner(IEnumerable<Seed> initialSeeds, bool forward, bool backward)
        {
            var seeds = new FastPriorityQueue<Seed>(1000);

            foreach (var initialSeed in initialSeeds)
                seeds.Enqueue(initialSeed, 1);

            //Trace out roads for every single seed
            while (seeds.Count > 0)
            {
                var s = seeds.Dequeue();

                if (forward)
                {
                    var stream = Trace(s, false, seeds);
                    if (stream != null)
                    {
                        streams.Add(stream);
                        //streamCreated(stream);
                    }
                }

                if (backward)
                {
                    var stream = Trace(s, true, seeds);
                    if (stream != null)
                    {
                        streams.Add(stream);
                        //streamCreated(stream);
                    }
                }
            }
        }



        public Streamline Trace(Seed seed, bool reverse, FastPriorityQueue<Seed> seeds)
        {
            float maxSegmentLength = 1;
            float mergeDistance = 25;
            float cosineSearchAngle = 22.5f;

            var maxSegmentLengthSquared = maxSegmentLength * maxSegmentLength;
            Streamline stream = new Streamline(new Vertex(seed.pos));

            var seedingDistance = float.MaxValue;
            var direction = Vector2.Zero;
            var position = seed.pos;
            bool tracingMajor = true;
            Vector2 prev_direction = Vector2.Zero;
            //var stream = new Streamline(FindOrCreateVertex(position, mergeDistance, cosineSearchAngle));

            for (var i = 0; i < 10000; i++)
            {
                Vector2 major, minor;
                Rk4_sample_field(out major, out minor, seed.pos, prev_direction, tensors, tracingMajor);
                direction = major;
                if (i == 0)
                    direction *= reverse ? -1 : 1;

                //degenerate step check
                var segmentLength = direction.Length();
                if (segmentLength < 0.00005f)
                    break;

                //Excessive step check
                if (segmentLength > maxSegmentLength)
                {
                    direction /= segmentLength * maxSegmentLength;
                    segmentLength = maxSegmentLength;
                }

                //Step along path
                Vector2 temp = position;
                position += direction;
                seedingDistance += segmentLength;

                //Bounds check
                if (position.X < 0 || position.Y < 0 || position.X > 1 || position.Y > 1)
                {
                    Vertex start = new Vertex(temp);
                    Vertex end = new Vertex(position);
                    Edge myedge = new Edge(stream, start, end);

                    myedge.MakeEdge(start, end, stream);

                    stream.vertices.Add(end);
                    all_vertices.Add(end);
                    all_edges.Add(myedge);

                    //CreateEdge(stream, position, Vector2.Normalize(direction), maxSegmentLength, maxSegmentLengthSquared, mergeDistance, cosineSearchAngle, skip: true);
                    break;
                }

                Vertex start = new Vertex(temp);
                Vertex end = new Vertex(position);
                Edge myedge = new Edge(stream, start, end);

                myedge.MakeEdge(start, end, stream);

                stream.vertices.Add(end);
                all_vertices.Add(end);
                all_edges.Add(myedge);

                //Create the segment and break if it says so
                //if (CreateEdge(stream, position, Vector2.Normalize(direction), maxSegmentLength, maxSegmentLengthSquared, mergeDistance, cosineSearchAngle))
                //    break;

                ////Accumulate seeds to trace into the alternative field
                //var seedSeparation = separation.Sample(position);
                //if (seedingDistance > seedSeparation)
                //{
                //    seedingDistance = 0;
                //    AddSeed(seeds, new Seed(position, seed.AlternativeField, seed.Field));
                //}

                prev_direction = direction;
            }

            return stream;
        }

        //This function was not created by us 
        bool CreateEdge(Streamline stream, Vector2 pos, Vector2 dir, float segmentLength, float segmentLengthSquared, float mergeDistance, float cosineSearchAngle, bool skip = false) {
            bool stop = false;

            //check if distance too short

            //check if another vertex nearby to use instead

            //check and handle intersection

            Vertex endVertex = new Vertex(pos);

            if (!stream.vertices.Contains(endVertex) || stream.first.Equals(endVertex))
            {
                
            }
            return stop;
        }
    }
}