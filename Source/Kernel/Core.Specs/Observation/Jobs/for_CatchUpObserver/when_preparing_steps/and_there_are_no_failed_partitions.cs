// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

public class and_there_are_no_failed_partitions : given.a_catch_up_observer_job
{
    static readonly Key _key1 = (Key)"partition-1";
    static readonly Key _key2 = (Key)"partition-2";
    IEnumerable<Key>? _capturedKeys;

    void Establish()
    {
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key1, _key2));
        _observer.When(o => o.RegisterCatchingUpPartitions(Arg.Any<IEnumerable<Key>>()))
            .Do(call => _capturedKeys = call.Arg<IEnumerable<Key>>().ToList());
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_register_all_keys_for_catching_up() => _capturedKeys.ShouldNotBeNull();
    [Fact] void should_include_first_partition() => _capturedKeys!.ShouldContain(_key1);
    [Fact] void should_include_second_partition() => _capturedKeys!.ShouldContain(_key2);
}
