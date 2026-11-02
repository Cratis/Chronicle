// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ClusterRoles;

public class when_using_defaults : Specification
{
    ChronicleOptions _options;

    void Because() => _options = new();

    [Fact] void should_enable_event_sequences() => _options.Clustering.Roles.EventSequences.ShouldBeTrue();
    [Fact] void should_enable_observers() => _options.Clustering.Roles.Observers.ShouldBeTrue();
    [Fact] void should_enable_jobs() => _options.Clustering.Roles.Jobs.ShouldBeTrue();
    [Fact] void should_fall_back_to_any_silo() => _options.Clustering.JobsFallback.ShouldEqual(JobsFallback.AnySilo);
    [Fact] void should_leave_the_non_jobs_budget_unset() => _options.Jobs.MaxParallelStepsWithoutJobsRole.ShouldBeNull();
}
