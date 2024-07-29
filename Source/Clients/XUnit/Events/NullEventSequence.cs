// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Events;

/// <summary>
/// Represents a null implementation of <see cref="IEventSequence"/> that does nothing.
/// </summary>
public class NullEventSequence : IEventSequence
{
    /// <inheritdoc/>
    public EventSequenceId Id => EventSequenceId.Log;

    /// <inheritdoc/>
    public Task Append(EventSourceId eventSourceId, object @event) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task AppendMany(EventSourceId eventSourceId, IEnumerable<object> events) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> eventTypes) => Task.FromResult<IImmutableList<AppendedEvent>>(ImmutableList<AppendedEvent>.Empty);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() => Task.FromResult(EventSequenceNumber.First);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber() => Task.FromResult(EventSequenceNumber.First);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) => Task.FromResult(EventSequenceNumber.First);

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason? reason = null) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Redact(EventSourceId eventSourceId, RedactionReason? reason = null, params Type[] eventTypes) => Task.CompletedTask;
}
