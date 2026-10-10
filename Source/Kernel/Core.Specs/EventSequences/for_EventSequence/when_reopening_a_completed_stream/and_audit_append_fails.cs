// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reopening_a_completed_stream;

public class and_audit_append_fails : given.a_repairable_sequence
{
    Exception _error;

    async Task Establish()
    {
        await _closures.Close(new(_scope, ClosedStreamOwner.Manual, EventSequenceNumber.First, DateTimeOffset.UtcNow));
        _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<Causation>?>(), Arg.Any<Identity?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>())
            .Returns(new AppendResult { Errors = [AppendError.Unknown] });
    }

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.ReopenCompletedStream(_scope, "Repair", CorrelationId.NotSet, [], _actor));

    [Fact] void should_fail_loudly() => _error.ShouldBeOfExactType<Sequences.ReopenStreamScopeAuditFailed>();
    [Fact] async Task should_preserve_the_manual_closure() => (await _closures.GetAll()).Single().Owner.ShouldEqual(ClosedStreamOwner.Manual);
}
