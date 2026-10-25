// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_registering_partition_ownership;

/// <summary>
/// A projection catches up through one step that carries no keys, so the key index is the only source for its partitions.
/// </summary>
public class and_the_observer_is_a_projection : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";
    static readonly Key _key3 = (Key)"partition-3";
    IEnumerable<Key>? _registered;

    void Establish()
    {
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key1, _key2, _key3));
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>([_key2]));
        _job.SetStateForTesting(_request, JobStatus.Running, []);
        _observer
            .When(_ => _.RegisterResumedCatchingUpPartitions(Arg.Any<JobId>(), Arg.Any<Guid>(), Arg.Any<IEnumerable<Key>>()))
            .Do(call => _registered = call.Arg<IEnumerable<Key>>().ToList());
    }

    async Task Because() => await _job.RegisterPartitionOwnership(_generation);

    [Fact] void should_register_the_keys_of_the_index_that_have_not_failed() => _registered.ShouldContainOnly(_key1, _key3);
}
