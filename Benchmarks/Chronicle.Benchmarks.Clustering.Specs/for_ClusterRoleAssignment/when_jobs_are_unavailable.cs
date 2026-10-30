// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ClusterRoleAssignment;

public class when_jobs_are_unavailable : Specification
{
    Exception? _error;
    void Because() => _error = Catch.Exception(() => ClusterRoleAssignment.JobsProperty(new object(), ClusterTopology.TwoSilosWithSplitJobs));
    [Fact] void should_refuse_the_jobs_split() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_identify_the_required_kernel_role() => _error!.Message.ShouldEqual("split-jobs needs the cluster Jobs role (Chronicle#5167)");
}
