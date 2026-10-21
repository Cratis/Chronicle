// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reopening_a_completed_stream.given;

public class a_repairable_sequence : when_completing_a_stream_scope.given.an_event_sequence_with_closed_streams
{
    protected readonly ClosedStreamScope _scope = new(EventSourceId: "source");
    protected readonly Identity _actor = new("operator", "Operator", "operator");
    protected IEventSequence _systemSequence;
    protected Result<Sequences.ReopenStreamScopeError> _result;
    protected bool _closedDuringAudit;

    void Establish()
    {
        _systemSequence = Substitute.For<IEventSequence>();
        _silo.AddProbe<IEventSequence>(_ => _systemSequence);
        _systemSequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<Causation>?>(), Arg.Any<Identity?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>())
            .Returns(async _ =>
            {
                _closedDuringAudit = (await _closures.GetAll()).Any(closure => closure.Scope == _scope && closure.Owner == ClosedStreamOwner.Manual);
                return AppendResult.Success(CorrelationId.NotSet, EventSequenceNumber.First);
            });
    }
}
