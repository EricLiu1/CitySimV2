//using System;
//using System.Collections.Generic;
//using System.Numerics;
//using Priority_Queue;

//namespace roadmap
//{
//    public class RoadBuilder
//    {
//        public static List<Edge> all_edges;
//        public static List<Vertex> all_vertices;
//        public HashSet<Streamline> streams;

//        public RoadBuilder()
//        {
//            streams = new HashSet<Streamline>();
//            all_edges = new List<Edge>();
//            all_vertices = new List<Vertex>();
//        }

//        public void SeedRunner(IEnumerable<Seed> initialSeeds, bool forward, bool backward)
//        {
//            var seeds = new FastPriorityQueue<Seed>(1000);

//            foreach (var initialSeed in initialSeeds)
//                seeds.Enqueue(initialSeed, 1);

//            //Trace out roads for every single seed
//            while (seeds.Count > 0)
//            {
//                var s = seeds.Dequeue();

//                if (forward)
//                {
//                    var stream = Trace(s, false, seeds);
//                    if (stream != null)
//                    {
//                        streams.Add(stream);
//                        //streamCreated(stream);
//                    }
//                }

//                if (backward)
//                {
//                    var stream = Trace(s, true, seeds);
//                    if (stream != null)
//                    {
//                        streams.Add(stream);
//                        //streamCreated(stream);
//                    }
//                }
//            }
//        }

//        public Streamline Trace(Seed seed, bool reverse, FastPriorityQueue<Seed> seeds)
//        {
//            float maxSegmentLength = 10;
//            float mergeDistance = 25;
//            float cosineSearchAngle = 22.5f;

//            var maxSegmentLengthSquared = maxSegmentLength * maxSegmentLength;

//            var seedingDistance = float.MaxValue;
//            var direction = Vector2.Zero;
//            var position = seed.pos;

//            //var stream = new Streamline(FindOrCreateVertex(position, mergeDistance, cosineSearchAngle));

//            for (var i = 0; i < 10000; i++)
//            {
//                direction = seed.Field.TraceVectorField(position, direction, maxSegmentLength);
//                if (i == 0)
//                    direction *= reverse ? -1 : 1;

//                //degenerate step check
//                var segmentLength = direction.Length();
//                if (segmentLength < 0.00005f)
//                    break;

//                //Excessive step check
//                if (segmentLength > maxSegmentLength)
//                {
//                    direction /= segmentLength * maxSegmentLength;
//                    segmentLength = maxSegmentLength;
//                }

//                //Step along path
//                position += direction;
//                seedingDistance += segmentLength;

//                //Bounds check
//                if (isOutOfBounds(position))
//                {
//                    CreateEdge(stream, position, Vector2.Normalize(direction), maxSegmentLength, maxSegmentLengthSquared, mergeDistance, cosineSearchAngle, skipDistanceCheck: true);
//                    break;
//                }

//                //Create the segment and break if it says so
//                if (CreateEdge(stream, position, Vector2.Normalize(direction), maxSegmentLength, maxSegmentLengthSquared, mergeDistance, cosineSearchAngle))
//                    break;

//                //Accumulate seeds to trace into the alternative field
//                var seedSeparation = separation.Sample(position);
//                if (seedingDistance > seedSeparation)
//                {
//                    seedingDistance = 0;
//                    AddSeed(seeds, new Seed(position, seed.AlternativeField, seed.Field));
//                }
//            }

//            return stream;
//        }
//    }
//}