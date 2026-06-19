// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_observer_is_not_a_projection : given.a_replay_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";
    IImmutableList<JobStepDetails> _steps;

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key1, _key2));
    }

    async Task Because() => _steps = await _job.PrepareStepsForTesting(_request);

    [Fact] void should_create_one_step_per_partition() => _steps.Count.ShouldEqual(2);
    [Fact] void should_use_partition_steps() => _steps.All(_ => _.Type == typeof(IHandleEventsForPartition)).ShouldBeTrue();
    [Fact] void should_include_first_partition() => ((HandleEventsForPartitionArguments)_steps[0].Request).Partition.ShouldEqual(_key1);
    [Fact] void should_include_second_partition() => ((HandleEventsForPartitionArguments)_steps[1].Request).Partition.ShouldEqual(_key2);
}

