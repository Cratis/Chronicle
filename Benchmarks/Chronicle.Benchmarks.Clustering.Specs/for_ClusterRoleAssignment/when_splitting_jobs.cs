// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ClusterRoleAssignment;

public class when_splitting_jobs : Specification
{
    ClusterRoleAssignment _first = null!;
    ClusterRoleAssignment _second = null!;
    void Because()
    {
        _first = ClusterRoleAssignment.For(ClusterTopology.TwoSilosWithSplitJobs, isFirstSilo: true);
        _second = ClusterRoleAssignment.For(ClusterTopology.TwoSilosWithSplitJobs, isFirstSilo: false);
    }
    [Fact] void should_keep_event_sequences_and_observers_on_silo_zero() => _first.ShouldEqual(new ClusterRoleAssignment(true, true, false));
    [Fact] void should_host_only_jobs_on_silo_one() => _second.ShouldEqual(new ClusterRoleAssignment(false, false, true));
}
