// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_preparing_a_subset_type_replay : given.a_partition_replay_job
{
    IImmutableList<JobStepDetails> _steps;

    void Establish()
    {
        _request = _request with { ReplaysAllEventTypes = false };
        _partitionRebuildAdmission.IsAdmitted(_request).Returns(false);
    }

    async Task Because() => _steps = await _job.PrepareStepsForTesting(_request);

    [Fact] void should_ask_admission_about_the_subset_replay() => _partitionRebuildAdmission.Received(1).IsAdmitted(Arg.Is<ReplayObserverPartitionRequest>(_ => !_.ReplaysAllEventTypes));
    [Fact] void should_replay_the_partition_in_place() => ((HandleEventsForPartitionArguments)_steps[0].Request).IsolatedRebuild.ShouldBeFalse();
}
