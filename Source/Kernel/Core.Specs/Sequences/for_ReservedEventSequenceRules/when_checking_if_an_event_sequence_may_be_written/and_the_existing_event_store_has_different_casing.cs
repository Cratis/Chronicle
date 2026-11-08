// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_the_existing_event_store_has_different_casing : given.a_reserved_name_check
{
    readonly EventStoreName _canonicalName = EventStore.ToUpperInvariant();
    bool _result;

    void Establish()
    {
        Reserve("observers");
        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([_canonicalName]));
        _namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([new EventSequenceId("observers")]));
    }

    async Task Because() => _result = await Check("observers");

    [Fact] void should_allow_the_existing_sequence() => _result.ShouldBeTrue();
    [Fact] void should_use_the_canonical_store_name() => _storage.Received(1).GetEventStore(_canonicalName);
    [Fact] void should_not_use_the_requested_store_name() => _storage.DidNotReceive().GetEventStore((EventStoreName)EventStore);
}
