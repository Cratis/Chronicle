// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Events;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Specifications.Types;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an <see cref="IEventLog"/> for working in-memory.
/// </summary>
public class EventLogForSpecifications : IEventLog
{
    static readonly IEventSerializer _serializer = new EventSerializer(new KnownInstancesOf<ICanProvideAdditionalEventInformation>(), new());
    readonly List<AppendedEventForSpecifications> _appendedEvents = new();
    EventSequenceNumber _sequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Gets the appended events.
    /// </summary>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _appendedEvents;

    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = null)
    {
        var json = await _serializer.Serialize(@event);

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
            json,
            @event));
        _sequenceNumber++;
    }
}
