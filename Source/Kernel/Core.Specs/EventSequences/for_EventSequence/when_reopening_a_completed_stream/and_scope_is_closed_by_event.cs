// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reopening_a_completed_stream;

public class and_scope_is_closed_by_event : given.a_repairable_sequence
{
    async Task Establish() => await _closures.Close(new(_scope, new ClosedStreamOwner("closing"), EventSequenceNumber.First, DateTimeOffset.UtcNow));

    async Task Because() => _result = await _eventSequence.ReopenCompletedStream(_scope, "Repair", CorrelationId.NotSet, [], _actor);

    [Fact] void should_reject_the_repair() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_closed_by_event() => _result.AsT1.ShouldEqual(ReopenStreamScopeError.ClosedByEvent);
    [Fact] async Task should_preserve_the_closure() => (await _closures.GetAll()).Count().ShouldEqual(1);
    [Fact] void should_not_audit_a_rejected_repair() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
}
