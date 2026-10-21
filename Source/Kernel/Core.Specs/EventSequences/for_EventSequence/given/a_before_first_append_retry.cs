// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_before_first_append_retry : appending_many_events
{
    protected int _attempts;
    protected int _scopeChecks;
    protected EventSequenceNumber _retryTail = EventSequenceNumber.Unavailable;
    protected CorrelationId _correlationId = CorrelationId.New();

    void Establish()
    {
        _events = [EventToAppendFor(_eventSourceId), EventToAppendFor(_eventSourceId), EventToAppendFor(_eventSourceId)];
        _eventSequenceStorage.GetTailSequenceNumber(null, _eventSourceId, null, null, null)
            .Returns(_ =>
            {
                _scopeChecks++;
                return _attempts == 0 ? EventSequenceNumber.Unavailable : _retryTail;
            });
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call =>
            {
                _attempts++;
                return Task.FromResult(_attempts == 1
                    ? (Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>)new DuplicateEventSequenceNumber(6)
                    : Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(call.Arg<IEnumerable<EventToAppendToStorage>>())));
            });
        _eventSequenceStorage.Append(
            Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<IEnumerable<IdentityId>>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<DateTimeOffset>(), Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>(), Arg.Any<IDictionary<EventTypeGeneration, EventHash>>(), Arg.Any<Subject?>())
            .Returns(call =>
            {
                _attempts++;
                return Task.FromResult(_attempts == 1
                    ? (Result<AppendedEvent, DuplicateEventSequenceNumber>)new DuplicateEventSequenceNumber(6)
                    : Result<AppendedEvent, DuplicateEventSequenceNumber>.Success(new AppendedEvent(
                        EventContext.From(EventStore, EventStoreNamespace, _eventType, EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, call.ArgAt<EventSequenceNumber>(0), _correlationId),
                        new ExpandoObject())));
            });
    }

    protected Task<AppendResult> AppendSingleWithScope() => _eventSequence.Append(
        EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, new JsonObject(), _correlationId, [], Identity.System, [], new ConcurrencyScope(EventSequenceNumber.BeforeFirst, true, null, null, null, null));

    protected Task<AppendManyResult> AppendBatchWithScope() => _eventSequence.AppendMany(
        _events, _correlationId, [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [_eventSourceId] = new(EventSequenceNumber.BeforeFirst, true, null, null, null, null)
        }));
}
