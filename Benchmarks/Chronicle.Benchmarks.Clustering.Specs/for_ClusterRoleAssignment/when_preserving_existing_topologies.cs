// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ClusterRoleAssignment;

public class when_preserving_existing_topologies : Specification
{
    ClusterRoleAssignment[] _assignments = null!;
    void Because() => _assignments = new[] { ClusterTopology.SingleSilo, ClusterTopology.TwoSilos, ClusterTopology.ThreeSilos }
        .SelectMany(topology => new[] { ClusterRoleAssignment.For(topology, isFirstSilo: true), ClusterRoleAssignment.For(topology, isFirstSilo: false) }).ToArray();
    [Fact] void should_leave_all_roles_enabled() => _assignments.All(roles => roles == new ClusterRoleAssignment(true, true, true)).ShouldBeTrue();
}
