// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_append_with_settlement : an_event_sequence_with_repairable_indexes
{
    protected bool _incomplete;
    protected bool _throwAfterWrite;
    protected bool _notCommitted;
    protected bool _empty;
    protected bool _probeFails;
    protected bool _foreign;
    protected bool _unsetHash;
    protected AppendManyResult _batchResult;
    protected Exception _error;
    protected int _probes;

    void Establish()
    {
        _eventHashCalculator.Calculate(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<ExpandoObject>()).Returns((EventHash)"submitted-hash");
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(async call =>
        {
            if (_notCommitted) throw new EventsNotCommitted(new SimulatedStorageError());
            if (_empty) throw new SimulatedStorageError();
            var result = await _log.AppendMany(call.Arg<IEnumerable<EventToAppendToStorage>>());
            if (_throwAfterWrite) throw new SimulatedStorageError();
            return _incomplete ? Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success([]) : result;
        });
        _eventSequenceStorage.Append(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<IEnumerable<IdentityId>>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<DateTimeOffset>(), Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>(), Arg.Any<IDictionary<EventTypeGeneration, EventHash>>(), Arg.Any<Subject?>())
            .Returns(async call =>
            {
                if (_notCommitted) throw new EventsNotCommitted(new SimulatedStorageError());
                var result = await _log.Append(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSourceType>(1), call.ArgAt<EventSourceId>(2), call.ArgAt<EventStreamType>(3), call.ArgAt<EventStreamId>(4), call.ArgAt<EventType>(5), call.ArgAt<CorrelationId>(6), call.ArgAt<IEnumerable<Causation>>(7), call.ArgAt<IEnumerable<IdentityId>>(8), call.ArgAt<IEnumerable<Tag>>(9), call.ArgAt<DateTimeOffset>(10), call.ArgAt<IDictionary<EventTypeGeneration, ExpandoObject>>(11), call.ArgAt<IDictionary<EventTypeGeneration, EventHash>>(12), call.ArgAt<Subject?>(13));
                if (_throwAfterWrite) throw new SimulatedStorageError();
                return result;
            });
        _eventSequenceStorage.ReadBackAppend(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _probes++;
            if (_probeFails) throw new SimulatedStorageError();
            var events = _log.Events.Where(e => e.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0) && e.Context.SequenceNumber <= call.ArgAt<EventSequenceNumber>(1))
                .Select(e => e with
                {
                    Context = _foreign ? e.Context with { CorrelationId = CorrelationId.New() } : e.Context,
                    GenerationalHashes = _unsetHash ? new Dictionary<int, EventHash> { [1] = EventHash.NotSet } : e.GenerationalHashes
                }).ToList();
            return Task.FromResult<IEventCursor>(new EventCursor(events));
        });
        _eventSequenceStorage.GetTailSequenceNumber().Returns(_ => _log.GetTailSequenceNumber());
    }

    protected async Task AppendBatch(bool governed = false)
    {
        var first = ValidatedEvent();
        var second = ValidatedEvent();
        if (governed)
        {
            first.Event = first.Event with { EventType = _eventType };
            second.Event = second.Event with { EventType = new("OtherEvent", EventTypeGeneration.First) };
            first.CompliantEvent = Content();
            second.CompliantEvent = Content();
            first.ConstraintContext = _currentValidation.Establish(first.Event.EventSourceId, first.Event.EventType.Id, first.CompliantEvent);
            second.ConstraintContext = _currentValidation.Establish(second.Event.EventSourceId, second.Event.EventType.Id, second.CompliantEvent);
        }
        else
        {
            first.ConstraintContext = new([_recordingValidator], first.Event.EventSourceId, first.Event.EventType.Id, first.CompliantEvent);
            second.ConstraintContext = new([_recordingValidator], second.Event.EventSourceId, second.Event.EventType.Id, second.CompliantEvent);
        }
        _error = await Cratis.Specifications.Catch.Exception(async () => _batchResult = await _eventSequence.AppendManyToStorage([first, second], CorrelationId.New(), [], []));
    }
}
