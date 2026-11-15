// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_using_a_reserved_name;

public class and_the_sequence_is_custom : given.an_event_sequence
{
    AppendResult _result;
    protected override EventSequenceId SequenceId => "orders";

    async Task Because()
    {
        await AppendAnEvent();
        _result = await AppendAnEvent();
    }

    [Fact] void should_append() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_check_the_provider_once() => _reservedEventSequenceIds.Received(1).IsReserved(SequenceId);
    [Fact] void should_not_enumerate_stores() => _storage.DidNotReceive().GetEventStores();
    [Fact] void should_not_enumerate_sequences() => _namespaceStorage.DidNotReceive().GetEventSequences();
}
