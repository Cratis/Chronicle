// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.given;

public class a_legacy_unit : IUnitOfWork
{
    public bool IsCompleted => false;
    public CorrelationId CorrelationId => CorrelationId.NotSet;
    public bool IsSuccess => true;
    public EventForEventSourceId? Event { get; set; }
    public ConcurrencyScope? Scope { get; set; }
    public bool NamedTagsUsed { get; set; }
    public bool BatchUsed { get; set; }

    public void AddEvent(EventSequenceId eventSequenceId, EventSourceId eventSourceId, object @event, Causation causation, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, ConcurrencyScope? concurrencyScope = default, IEnumerable<string>? tags = default, DateTimeOffset? occurred = default, Subject? subject = default)
    {
        Event = new(eventSourceId, @event, causation)
        {
            EventStreamType = eventStreamType ?? EventStreamType.All,
            EventStreamId = eventStreamId ?? EventStreamId.Default,
            EventSourceType = eventSourceType ?? EventSourceType.Default,
            Tags = tags ?? [],
            Occurred = occurred,
            Subject = subject
        };
        Scope = concurrencyScope;
    }

    public void AddEventWithNamedTags(EventSequenceId eventSequenceId, EventSourceId eventSourceId, object @event, IEnumerable<NamedTag> namedTags, Causation causation, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, ConcurrencyScope? concurrencyScope = default, IEnumerable<string>? tags = default, DateTimeOffset? occurred = default, Subject? subject = default)
    {
        AddEvent(eventSequenceId, eventSourceId, @event, causation, eventStreamType, eventStreamId, eventSourceType, concurrencyScope, tags, occurred, subject);
        NamedTagsUsed = true;
        Event = Event! with { NamedTags = namedTags.ToArray() };
    }

    public void AddEvents(EventSequenceId eventSequenceId, IEnumerable<EventForEventSourceId> events, IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes)
    {
        BatchUsed = true;
        Event = events.Single();
        Scope = concurrencyScopes.Single().Value;
    }

    public IEnumerable<object> GetEvents() => [];
    public IEnumerable<ConstraintViolation> GetConstraintViolations() => [];
    public IEnumerable<ConcurrencyViolation> GetConcurrencyViolations() => [];
    public IEnumerable<AppendError> GetAppendErrors() => [];
    public Task Commit() => Task.CompletedTask;
    public Task Rollback() => Task.CompletedTask;
    public void OnCompleted(Action<IUnitOfWork> callback) { }
    public bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)] out EventSequenceNumber? eventSequenceNumber)
    {
        eventSequenceNumber = null;
        return false;
    }
    public void Dispose() { }
}
