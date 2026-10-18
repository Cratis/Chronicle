// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_a_constraint_is_pending_and_the_key_is_durable : given.an_event_sequence_with_idempotency_keys
{
    void Establish()
    {
        PendingConstraint();
        Durable();
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_not_refuse_the_durable_key() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_a_hit() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] async Task should_not_start_repair() => await _eventSequenceStorage.DidNotReceive().GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>());
}
