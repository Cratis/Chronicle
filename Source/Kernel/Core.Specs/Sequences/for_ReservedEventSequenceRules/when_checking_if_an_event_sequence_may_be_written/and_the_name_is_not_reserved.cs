// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_the_name_is_not_reserved : given.a_reserved_name_check
{
    bool _result;

    async Task Because() => _result = await Check("orders");

    [Fact] void should_be_allowed() => _result.ShouldBeTrue();
    [Fact] void should_not_look_for_the_event_store() => _storage.DidNotReceive().HasEventStore(Arg.Any<EventStoreName>());
    [Fact] void should_not_read_the_event_store() => _storage.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>());
    [Fact] void should_not_read_existing_event_sequences() => _namespaceStorage.DidNotReceive().GetEventSequences();
}
