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
    readonly Func<long> _getAppendedEventsCount;
    readonly Counter<int> _appendedEvents;
    readonly Counter<int> _duplicateEventSequenceNumbers;
    readonly Counter<int> _failedAppendedEvents;

    /// <summary>
    /// Initializes a new instance of <see cref="EventSequenceMetrics"/>.
    /// </summary>
    /// <param name="meter">The global <see cref="Meter"/>.</param>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> the metrics is for.</param>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the metrics is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the metrics is for.</param>
    /// <param name="getAppendedEventsCount">Callback for getting total events count.</param>
    public EventSequenceMetrics(
        Meter meter,
        EventSequenceId eventSequenceId,
        MicroserviceId microserviceId,
        TenantId tenantId,
        Func<long> getAppendedEventsCount)
    {
        _eventSequenceId = eventSequenceId;
        _microserviceId = microserviceId;
        _tenantId = tenantId;
        _getAppendedEventsCount = getAppendedEventsCount;
        _appendedEvents = meter.CreateCounter<int>("cratis-event_sequences-appended-events", "Number of events appended to the event sequence");
        meter.CreateObservableCounter("cratis-event-sequences-appended-events-total", GetTotalAppendedEvents, "Total number of events appended to the event sequence");
        _duplicateEventSequenceNumbers = meter.CreateCounter<int>("cratis-event-sequences-duplicate-event-sequence-numbers", "Number of duplicate sequence numbers");
        _failedAppendedEvents = meter.CreateCounter<int>("cratis-event-sequences-failed-appended-events", "Number of events that failed to be appended to the event sequence");
    }

    /// <inheritdoc/>
    public void AppendedEvent(EventSourceId eventSourceId, string eventName) => _appendedEvents.Add(1, GetTagsFor(eventSourceId, eventName));

    /// <inheritdoc/>
    public void DuplicateEventSequenceNumber(EventSourceId eventSourceId, string eventName) => _duplicateEventSequenceNumbers.Add(1, GetTagsFor(eventSourceId, eventName));

    /// <inheritdoc/>
    public void FailedAppending(EventSourceId eventSourceId, string eventName) => _failedAppendedEvents.Add(1, GetTagsFor(eventSourceId, eventName));

    Measurement<long> GetTotalAppendedEvents()
    {
        var total = _getAppendedEventsCount();
        return new Measurement<long>(total, new TagList(new ReadOnlySpan<KeyValuePair<string, object?>>(new KeyValuePair<string, object?>[]
        {
            new("event_sequence_id", _eventSequenceId.ToString()),
            new("microservice_id", _microserviceId.ToString()),
            new("tenant_id", _tenantId.ToString())
        })));
    }

    TagList GetTagsFor(EventSourceId eventSourceId, string eventName) => new(new ReadOnlySpan<KeyValuePair<string, object?>>(new KeyValuePair<string, object?>[]
    {
        new("event_sequence_id", _eventSequenceId.ToString()),
        new("microservice_id", _microserviceId.ToString()),
        new("tenant_id", _tenantId.ToString()),
        new("event_source_id", eventSourceId.ToString()),
        new("event_name", eventName)
    }));
}
