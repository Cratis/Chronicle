// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_the_event_store_name_is_ambiguous : given.a_reserved_name_check
{
    bool _result;

    void Establish()
    {
        Reserve("observers");
        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([(EventStoreName)EventStore.ToUpperInvariant(), (EventStoreName)"Some-Event-Store"]));
        _namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([new EventSequenceId("observers")]));
    }

    async Task Because() => _result = await Check("observers");

    [Fact] void should_be_refused() => _result.ShouldBeFalse();
    [Fact] void should_not_open_or_create_a_store() => _storage.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>());
}
