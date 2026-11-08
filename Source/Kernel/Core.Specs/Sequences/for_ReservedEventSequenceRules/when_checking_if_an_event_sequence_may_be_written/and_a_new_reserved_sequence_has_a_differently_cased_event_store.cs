// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_a_new_reserved_sequence_has_a_differently_cased_event_store : given.a_reserved_name_check
{
    bool _result;

    void Establish()
    {
        Reserve("observers");
        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([(EventStoreName)EventStore.ToUpperInvariant()]));
    }

    async Task Because() => _result = await Check("observers");

    [Fact] void should_be_refused() => _result.ShouldBeFalse();
    [Fact] void should_use_the_canonical_store_name() => _storage.Received(1).GetEventStore((EventStoreName)EventStore.ToUpperInvariant());
}
