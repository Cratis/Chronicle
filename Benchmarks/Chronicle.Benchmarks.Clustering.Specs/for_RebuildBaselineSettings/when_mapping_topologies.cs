// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineSettings;

public class when_mapping_topologies : Specification
{
    readonly ClusterTopology[] _expected = [ClusterTopology.SingleSilo, ClusterTopology.TwoSilos, ClusterTopology.TwoSilosWithSplitJobs, ClusterTopology.ThreeSilos];
    ClusterTopology[] _topologies = null!;
    void Because() => _topologies = new[] { "single", "two", "split-jobs", "three" }
        .Select(name => new RebuildBaselineSettings("S1", "cold", 100, 5156, name, 200, 240).Cluster).ToArray();
    [Fact] void should_map_every_supported_topology() => _topologies.ShouldEqual(_expected);
}
