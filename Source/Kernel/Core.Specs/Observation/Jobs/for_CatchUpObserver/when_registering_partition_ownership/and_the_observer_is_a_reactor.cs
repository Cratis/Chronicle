// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_registering_partition_ownership;

/// <summary>
/// The persisted step plan is exactly the set the job prepared, so it - not the key index - says which partitions the job owns.
/// </summary>
public class and_the_observer_is_a_reactor : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";
    static readonly Key _key3 = (Key)"partition-3";
    IEnumerable<Key>? _registered;

    void Establish()
    {
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key1, _key2, _key3));
        _job.SetStateForTesting(
            new(_request.ObserverKey, ObserverType.Reactor, _request.FromEventSequenceNumber, []),
            JobStatus.Running,
            [PlannedPartitionStep(_key1), PlannedPartitionStep(_key2)]);
        _observer
            .When(_ => _.RegisterResumedCatchingUpPartitions(Arg.Any<JobId>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<Key>>()))
            .Do(call => _registered = call.Arg<IEnumerable<Key>>().ToList());
    }

    async Task Because() => await _job.RegisterPartitionOwnership(_generation);

    [Fact] void should_register_the_partitions_of_the_plan() => _registered.ShouldNotBeNull();
    [Fact] void should_register_exactly_the_planned_partitions() => _registered.ShouldContainOnly(_key1, _key2);
    [Fact] async Task should_register_them_for_this_job() => await _observer.Received(1).RegisterResumedCatchingUpPartitions(_jobId, _generation, Arg.Any<IEnumerable<Key>>());
}
