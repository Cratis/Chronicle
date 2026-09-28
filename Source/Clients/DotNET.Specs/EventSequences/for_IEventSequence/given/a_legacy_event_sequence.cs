// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_IEventSequence.given;

public class a_legacy_event_sequence : Specification
{
    protected LegacyEventSequence _implementation;
    protected IEventSequence _sequence;

    void Establish()
    {
        _implementation = new();
        _sequence = _implementation;
    }

    protected sealed class LegacyEventSequence : IEventSequence
    {
        public int AppendCalls { get; private set; }
        public int SingleSourceBatchCalls { get; private set; }
        public int RoutedBatchCalls { get; private set; }
        public EventSequenceId Id => EventSequenceId.Log;
        public IObservable<IEnumerable<AppendedEventWithResult>> AppendOperations => null!;
        public ITransactionalEventSequence Transactional => null!;

        public Task<AppendResult> Append(EventSourceId eventSourceId, object @event, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, ConcurrencyScope? concurrencyScope = default, DateTimeOffset? occurred = default, Subject? subject = default)
        {
            AppendCalls++;
            return Task.FromResult(new AppendResult());
        }

        public Task<AppendManyResult> AppendMany(EventSourceId eventSourceId, IEnumerable<object> events, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, ConcurrencyScope? concurrencyScope = default, DateTimeOffset? occurred = default, Subject? subject = default)
        {
            SingleSourceBatchCalls++;
            return Task.FromResult(new AppendManyResult());
        }

        public Task<AppendManyResult> AppendMany(IEnumerable<EventForEventSourceId> events, CorrelationId? correlationId = default, IEnumerable<string>? tags = default, IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default)
        {
            RoutedBatchCalls++;
            return Task.FromResult(new AppendManyResult());
        }

        public Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> filterEventTypes, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default) => throw new NotImplementedException();
        public Task<bool> HasEventsFor(EventSourceId eventSourceId) => throw new NotImplementedException();
        public Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? filterEventTypes = default) => throw new NotImplementedException();
        public Task<EventSequenceNumber> GetNextSequenceNumber() => throw new NotImplementedException();
        public Task<EventSequenceNumber> GetTailSequenceNumber(EventSourceId? eventSourceId = default, EventSourceType? eventSourceType = default, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, IEnumerable<EventType>? filterEventTypes = default) => throw new NotImplementedException();
        public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) => throw new NotImplementedException();
        public Task Revise(EventSequenceNumber sequenceNumber, object @event) => throw new NotImplementedException();
        public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason) => throw new NotImplementedException();
        public Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes) => throw new NotImplementedException();
        public Task<Result<EventSequenceNumber, CompleteStreamError>> CompleteStream(EventStreamType eventStreamType, EventStreamId eventStreamId) => throw new NotImplementedException();
    }
}
