// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_before_first_batch : appending_many_events
{
    protected AppendManyResult _result;
    protected CorrelationId _correlationId = CorrelationId.New();
    protected Dictionary<EventSourceId, ConcurrencyScope> _scopes;
    protected List<EventToAppend> _history = [];
    protected List<EventSourceId?> _checkedSources = [];
    protected EventToAppendToStorage[] _storedBatch = [];
    protected int _checksBeforeWrite;

    void Establish()
    {
        _events = [EventToAppendFor(_eventSourceId), EventToAppendFor(_eventSourceId), EventToAppendFor(_eventSourceId)];
        _scopes = new() { [_eventSourceId] = new(EventSequenceNumber.BeforeFirst, true, null, null, null, null) };
        _eventSequenceStorage.GetTailSequenceNumber(
            Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>())
            .Returns(call =>
            {
                var eventTypes = call.ArgAt<IEnumerable<EventType>?>(0)?.ToArray();
                var source = call.ArgAt<EventSourceId?>(1);
                var sourceType = call.ArgAt<EventSourceType?>(2);
                var stream = call.ArgAt<EventStreamId?>(3);
                var streamType = call.ArgAt<EventStreamType?>(4);
                _checkedSources.Add(source);
                return _history.Exists(@event =>
                    (eventTypes is not { Length: > 0 } || eventTypes.Any(eventType => eventType.Id == @event.EventType.Id)) &&
                    (source?.IsSpecified != true || @event.EventSourceId == source) &&
                    (sourceType?.IsDefaultOrUnspecified != false || @event.EventSourceType == sourceType) &&
                    (stream?.IsDefault != false || @event.eventStreamId == stream) &&
                    (streamType?.IsAll != false || @event.eventStreamType == streamType))
                    ? (EventSequenceNumber)5 : EventSequenceNumber.Unavailable;
            });
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call =>
            {
                _checksBeforeWrite = _checkedSources.Count;
                _storedBatch = call.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
                _history.AddRange(_events);
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(_storedBatch)));
            });
    }

    protected Task<AppendManyResult> AppendWithScopes() => _eventSequence.AppendMany(
        _events, _correlationId, [], Identity.System, new ConcurrencyScopes(_scopes));
}
