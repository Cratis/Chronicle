// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ClusterRoleAssignment;

public class when_jobs_are_available : Specification
{
    readonly given.JobsRoles _first = new();
    readonly given.JobsRoles _second = new();
    void Because()
    {
        var first = ClusterRoleAssignment.For(ClusterTopology.TwoSilosWithSplitJobs, isFirstSilo: true);
        var second = ClusterRoleAssignment.For(ClusterTopology.TwoSilosWithSplitJobs, isFirstSilo: false);
        ClusterRoleAssignment.JobsProperty(_first, ClusterTopology.TwoSilosWithSplitJobs)!.SetValue(_first, first.Jobs);
        ClusterRoleAssignment.JobsProperty(_second, ClusterTopology.TwoSilosWithSplitJobs)!.SetValue(_second, second.Jobs);
    }
    [Fact] void should_disable_jobs_on_silo_zero_via_reflection() => _first.Jobs.ShouldBeFalse();
    [Fact] void should_enable_jobs_on_silo_one_via_reflection() => _second.Jobs.ShouldBeTrue();
}
