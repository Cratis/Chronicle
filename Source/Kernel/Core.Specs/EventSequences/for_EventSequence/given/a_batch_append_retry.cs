// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_batch_append_retry : appending_many_events
{
    protected AppendManyResult _result;
    protected int _attempts;
    protected int _scopeChecks;
    protected EventSequenceNumber _retryTail = 3;
    protected readonly EventSourceId _readSource = "read-source";
    protected CorrelationId _correlationId = CorrelationId.New();

    void Establish()
    {
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _readSource, null, null, null)
            .Returns(_ =>
            {
                _scopeChecks++;
                return _attempts == 0 ? (EventSequenceNumber)3 : _retryTail;
            });
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call =>
            {
                _attempts++;
                return Task.FromResult(_attempts == 1
                    ? (Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>)new DuplicateEventSequenceNumber(6)
                    : Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(call.Arg<IEnumerable<EventToAppendToStorage>>())));
            });
    }

    protected Task<AppendManyResult> AppendWithScope() => _eventSequence.AppendMany(
        _events, _correlationId, [], Identity.System, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope> { [_readSource] = new(3, true, null, null, null, [_eventType]) }));
}
