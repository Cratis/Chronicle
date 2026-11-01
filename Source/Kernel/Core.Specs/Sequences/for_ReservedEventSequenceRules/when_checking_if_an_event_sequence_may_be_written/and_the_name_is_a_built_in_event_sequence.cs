// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_the_name_is_a_built_in_event_sequence : given.a_reserved_name_check
{
    bool[] _results;

    void Establish() => _reservedEventSequenceIds.IsReserved(Arg.Any<EventSequenceId>()).Returns(true);

    async Task Because() => _results =
    [
        await Check("event-log"),
        await Check("system"),
        await Check("outbox"),
        await Check("inbox"),
        await Check("inbox-other-store")
    ];

    [Fact] void should_allow_all_of_them() => _results.All(_ => _).ShouldBeTrue();
    [Fact] void should_not_read_existing_event_sequences() => _namespaceStorage.DidNotReceive().GetEventSequences();
}
