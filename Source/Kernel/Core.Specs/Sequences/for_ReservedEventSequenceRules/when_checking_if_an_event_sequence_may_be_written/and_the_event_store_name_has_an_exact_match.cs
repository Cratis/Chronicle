// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_the_event_store_name_has_an_exact_match : given.a_reserved_name_check
{
    readonly EventStoreName _otherName = EventStore.ToUpperInvariant();
    bool _result;

    void Establish()
    {
        Reserve("observers");
        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([_otherName, (EventStoreName)EventStore]));
        _namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([new EventSequenceId("observers")]));
    }

    async Task Because() => _result = await Check("observers");

    [Fact] void should_be_allowed() => _result.ShouldBeTrue();
    [Fact] void should_use_the_exact_match() => _storage.Received(1).GetEventStore((EventStoreName)EventStore);
    [Fact] void should_not_use_the_case_insensitive_match() => _storage.DidNotReceive().GetEventStore(_otherName);
}
