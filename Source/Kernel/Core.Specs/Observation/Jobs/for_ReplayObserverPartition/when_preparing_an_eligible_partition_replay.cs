// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_preparing_an_eligible_partition_replay : given.a_partition_replay_job
{
    IImmutableList<JobStepDetails> _steps;

    void Establish() => _partitionRebuildAdmission.IsAdmitted(_request).Returns(true);

    async Task Because() => _steps = await _job.PrepareStepsForTesting(_request);

    [Fact] void should_prepare_one_step() => _steps.Count.ShouldEqual(1);
    [Fact] void should_rebuild_the_partition_in_isolation() => ((HandleEventsForPartitionArguments)_steps[0].Request).IsolatedRebuild.ShouldBeTrue();
}
