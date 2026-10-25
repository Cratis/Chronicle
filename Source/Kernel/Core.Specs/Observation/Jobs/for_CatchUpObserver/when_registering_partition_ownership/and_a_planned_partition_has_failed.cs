// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_registering_partition_ownership;

public class and_a_planned_partition_has_failed : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";
    IEnumerable<Key>? _registered;

    void Establish()
    {
        _job.SetStateForTesting(
            new(_request.ObserverKey, ObserverType.Reactor, _request.FromEventSequenceNumber, []),
            JobStatus.Running,
            [PlannedPartitionStep(_key1), PlannedPartitionStep(_key2)]);
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>([_key2]));
        _observer
            .When(_ => _.RegisterResumedCatchingUpPartitions(Arg.Any<JobId>(), Arg.Any<IEnumerable<Key>>()))
            .Do(call => _registered = call.Arg<IEnumerable<Key>>().ToList());
    }

    async Task Because() => await _job.RegisterPartitionOwnership();

    [Fact] void should_register_only_the_partition_that_has_not_failed() => _registered.ShouldContainOnly(_key1);
}
