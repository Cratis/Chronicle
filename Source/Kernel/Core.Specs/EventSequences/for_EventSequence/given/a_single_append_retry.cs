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

public class a_single_append_retry : an_event_sequence
{
    protected AppendResult _result;
    protected int _attempts;
    protected int _scopeChecks;
    protected EventSequenceNumber _retryTail = 3;
    protected CorrelationId _correlationId = CorrelationId.New();

    void Establish()
    {
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _eventSourceId, Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>())
            .Returns(_ =>
            {
                _scopeChecks++;
                return _attempts == 0 ? (EventSequenceNumber)3 : _retryTail;
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

    protected Task<AppendResult> AppendWithScope() => _eventSequence.Append(
        EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, new JsonObject(), _correlationId, [], Identity.System, [], new ConcurrencyScope(3, true, null, null, null, [_eventType]));
}
