// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reopening_a_completed_stream;

public class and_only_a_broader_manual_scope_is_closed : given.a_repairable_sequence
{
    async Task Establish() => await _closures.Close(new(_scope, ClosedStreamOwner.Manual, EventSequenceNumber.First, DateTimeOffset.UtcNow));

    async Task Because() => _result = await _eventSequence.ReopenCompletedStream(_scope with { EventStreamType = new("orders") }, "Repair", CorrelationId.NotSet, [], _actor);

    [Fact] void should_report_not_completed() => _result.AsT1.ShouldEqual(ReopenStreamScopeError.NotCompleted);
    [Fact] async Task should_preserve_the_broader_closure() => (await _closures.GetAll()).Single().Scope.ShouldEqual(_scope);
    [Fact] void should_not_audit_a_rejected_repair() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
}
