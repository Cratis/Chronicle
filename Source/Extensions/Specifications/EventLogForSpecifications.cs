// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an <see cref="IEventLog"/> for working in-memory.
/// </summary>
public class EventLogForSpecifications : IEventLog
{
    readonly List<AppendedEvent> _appendedEvents = new();
    readonly List<object> _actualEvents = new();

    EventSequenceNumber _sequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Gets the appended events.
    /// </summary>
    public IEnumerable<AppendedEvent> AppendedEvents => _appendedEvents;

    /// <summary>
    /// Gets the actual events that was appended.
    /// </summary>
    public IEnumerable<object> ActualEvents => _actualEvents;

    /// <inheritdoc/>
    public Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = null)
    {
        var json = (JsonSerializer.SerializeToNode(@event) as JsonObject)!;

        var eventTypeAttribute = @event.GetType().GetCustomAttribute<EventTypeAttribute>();
        _appendedEvents.Add(new(
            new(_sequenceNumber, eventTypeAttribute!.Type),
            new(
                eventSourceId,
                DateTimeOffset.UtcNow,
                DateTimeOffset.MinValue,
                TenantId.Development,
                CorrelationId.New(),
                CausationId.System,
                CausedBy.System),
            json));
        _actualEvents.Add(@event);
        _sequenceNumber++;
        return Task.CompletedTask;
    }
}
