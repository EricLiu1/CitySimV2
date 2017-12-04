using System;
using System.Collections.Generic;
using System.Collections.Generic.SortedSet;
using System.Numerics;

namespace roadmap
{
    class SeedComparer : IComparer<Seed>
    {
        public int Compare(Seed x, Seed y) => x.priority - y.priority;
    }

    public class RoadBuilder
    {
        public static List<Edge> all_edges;
        public static List<Vertex> all_vertices;
        public HashSet<Streamline> streams;

        public RoadBuilder()
        {
            streams = new HashSet<Streamline>();
            all_edges = new List<Edge>();
            all_vertices = new List<Vertex>();
        }

        public void SeedRunner(IEnumerable<Seed> initialSeeds, bool forward, bool backward) {
            var comparer = new SeedComparer();
            var seeds = new SortedSet<Seed>(comparer);

            foreach (var initialSeed in initialSeeds)
                AddSeed(seeds, initialSeed);

            //Trace out roads for every single seed
            while (seeds.Count > 0)
            {
                var s = RemoveSeed(seeds, separation, cosineSearchAngle, edgeFilter);
                if (!s.HasValue)
                    continue;

                if (forward)
                {
                    var stream = CheckStream(Trace(s.Value, false, seeds, isOutOfBounds, maxSegmentLength, mergeDistance, cosineSearchAngle, separation));
                    if (stream != null)
                    {
                        _streams.Add(stream);
                        streamCreated(stream);
                    }
                }

                if (backward)
                {
                    var stream = CheckStream(Trace(s.Value, true, seeds, isOutOfBounds, maxSegmentLength, mergeDistance, cosineSearchAngle, separation));
                    if (stream != null)
                    {
                        _streams.Add(stream);
                        streamCreated(stream);
                    }
                }
            }
        }
    }
}
