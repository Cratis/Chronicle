// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceMetrics"/>.
/// </summary>
public class EventSequenceMetrics : IEventSequenceMetrics
{
    readonly EventSequenceId _eventSequenceId;
    readonly MicroserviceId _microserviceId;
    readonly TenantId _tenantId;

    readonly Counter<int> _appendedEvents;
    readonly Counter<int> _duplicateEventSequenceNumbers;
    readonly Counter<int> _failedAppendedEvents;

    /// <summary>
    /// Initializes a new instance of <see cref="EventSequenceMetrics"/>.
    /// </summary>
    /// <param name="meter"></param>
    /// <param name="eventSequenceId"></param>
    /// <param name="microserviceId"></param>
    /// <param name="tenantId"></param>
    public EventSequenceMetrics(Meter meter, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId)
    {
        _eventSequenceId = eventSequenceId;
        _microserviceId = microserviceId;
        _tenantId = tenantId;

        _appendedEvents = meter.CreateCounter<int>("cratis-event_sequences-appended-events", "Number of events appended to the event sequence");
        _duplicateEventSequenceNumbers = meter.CreateCounter<int>("cratis-event-sequences-duplicate-event-sequence-numbers", "Number of duplicate sequence numbers");
        _failedAppendedEvents = meter.CreateCounter<int>("cratis-event-sequences-failed-appended-events", "Number of events that failed to be appended to the event sequence");
    }

    /// <inheritdoc/>
    public void AppendedEvent(EventSourceId eventSourceId, string eventName) => _appendedEvents.Add(1, GetTagsFor(eventSourceId, eventName));

    /// <inheritdoc/>
    public void DuplicateEventSequenceNumber(EventSourceId eventSourceId, string eventName) => _duplicateEventSequenceNumbers.Add(1, GetTagsFor(eventSourceId, eventName));

    /// <inheritdoc/>
    public void FailedAppending(EventSourceId eventSourceId, string eventName) => _failedAppendedEvents.Add(1, GetTagsFor(eventSourceId, eventName));

    TagList GetTagsFor(EventSourceId eventSourceId, string eventName) => new(new ReadOnlySpan<KeyValuePair<string, object?>>(new KeyValuePair<string, object?>[] {
        new("event_sequence_id", _eventSequenceId.ToString()),
        new("microservice_id", _microserviceId.ToString()),
        new("tenant_id", _tenantId.ToString()),
        new("event_source_id", eventSourceId.ToString()),
        new("event_name", eventName)
    }));
}
