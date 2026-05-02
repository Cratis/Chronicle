// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

public class and_there_are_failed_partitions : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";
    static readonly Key _key3 = (Key)"partition-3";
    IEnumerable<Key>? _capturedKeys;

    void Establish()
    {
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key1, _key2, _key3));
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>([_key2]));
        _observer.When(o => o.RegisterCatchingUpPartitions(Arg.Any<IEnumerable<Key>>()))
            .Do(call => _capturedKeys = call.Arg<IEnumerable<Key>>().ToList());
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_register_only_non_failed_keys() => _capturedKeys.ShouldNotBeNull();
    [Fact] void should_include_first_partition() => _capturedKeys!.ShouldContain(_key1);
    [Fact] void should_exclude_failed_partition() => _capturedKeys!.ShouldNotContain(_key2);
    [Fact] void should_include_third_partition() => _capturedKeys!.ShouldContain(_key3);
}
