// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reopening_a_completed_stream;

public class and_scope_is_manually_closed : given.a_repairable_sequence
{
    async Task Establish() => await _closures.Close(new(_scope, ClosedStreamOwner.Manual, EventSequenceNumber.First, DateTimeOffset.UtcNow));

    async Task Because() => _result = await _eventSequence.ReopenCompletedStream(_scope, "Repair", CorrelationId.NotSet, [], _actor);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_remove_the_manual_closure() => (await _closures.GetAll()).ShouldBeEmpty();
    [Fact] void should_keep_the_closure_until_the_audit_is_durable() => _closedDuringAudit.ShouldBeTrue();
    [Fact] async Task should_audit_the_reason_and_actor() => await _systemSequence.Received(1).Append(Arg.Any<EventSourceId>(), Arg.Is<StreamScopeReopened>(audit => audit.Reason == "Repair" && audit.EventSourceId == "source"), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<Causation>?>(), Arg.Is<Identity>(actor => actor == _actor));
}
