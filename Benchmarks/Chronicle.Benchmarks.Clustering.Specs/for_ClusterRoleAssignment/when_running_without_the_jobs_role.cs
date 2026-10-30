// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_ClusterRoleAssignment;

public class when_running_without_the_jobs_role : Specification
{
    System.Reflection.PropertyInfo? _property;
    void Because() => _property = ClusterRoleAssignment.JobsProperty(new object(), ClusterTopology.TwoSilos);
    [Fact] void should_allow_existing_kernels_for_other_topologies() => _property.ShouldBeNull();
}
