// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Orleans.Execution;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueCacheCursor"/> for event sequences.
/// </summary>
public class EventSequenceQueueCacheCursor : IQueueCacheCursor
{
    readonly IEventSequenceCache _cache;
    readonly MicroserviceId _microserviceId;
    readonly TenantId _tenantId;
    readonly EventSequenceId _eventSequenceId;
    readonly ILogger _logger;
    readonly QueueId _queueId;
    AppendedEvent[] _events;
    EventSequenceNumber _from = EventSequenceNumber.Unavailable;
    EventSequenceNumber _to = EventSequenceNumber.Unavailable;
    int _currentIndex;
    EventSequenceNumber _previousEventSequenceNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCacheCursor"/> class.
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the cursor is for.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the cursor is for.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the cursor is for.</param>
    /// <param name="from">The from <see cref="EventSequenceNumber"/>.</param>
    /// <param name="logger"></param>
    /// <param name="queueId"></param>
    public EventSequenceQueueCacheCursor(
        IEventSequenceCache cache,
        MicroserviceId microserviceId,
        TenantId tenantId,
        EventSequenceId eventSequenceId,
        EventSequenceNumber from,
        ILogger logger,
        QueueId queueId)
    {
        _cache = cache;
        _microserviceId = microserviceId;
        _tenantId = tenantId;
        _eventSequenceId = eventSequenceId;
        _logger = logger;
        _queueId = queueId;
        _currentIndex = -1;
        _previousEventSequenceNumber = EventSequenceNumber.Unavailable;
        _events = Array.Empty<AppendedEvent>();
        logger.LogInformation("EventSequenceQueueCacheCursor ({QueueId}) created for {MicroserviceId}:{TenantId}:{EventSequenceId} from {From}", _queueId, microserviceId, tenantId, eventSequenceId, from);

        GetEventsFromCache(from);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _events = null!;
        _cache.Dispose();
    }

    /// <inheritdoc/>
    public IBatchContainer GetCurrent(out Exception exception)
    {
        exception = null!;
        try
        {
            var @event = _events[_currentIndex];
            _logger.LogInformation("Getting current ({QueueId}) for {MicroserviceId}:{TenantId}:{EventSequenceId} at {EventSequenceNumber}", _queueId, _microserviceId, _tenantId, _eventSequenceId, @event.Metadata.SequenceNumber);
            return new EventSequenceBatchContainer(
                new[] { @event },
                _eventSequenceId,
                _microserviceId,
                _tenantId,
                new Dictionary<string, object>
                {
                { RequestContextKeys.MicroserviceId, _microserviceId },
                { RequestContextKeys.TenantId, _tenantId },
                { RequestContextKeys.CorrelationId, @event.Context.CorrelationId },
                { RequestContextKeys.CausationId, @event.Context.CausationId },
                { RequestContextKeys.CausedBy, @event.Context.CausedBy }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current ({QueueId}) for {MicroserviceId}:{TenantId}:{EventSequenceId}", _queueId, _microserviceId, _tenantId, _eventSequenceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (_to == EventSequenceNumber.Unavailable)
        {
            _logger.LogInformation("MoveNext ({QueueId}) to is set to unavailable for {MicroserviceId}:{TenantId}:{EventSequenceId} - no more events", _queueId, _microserviceId, _tenantId, _eventSequenceId);
            return false;
        }

        // If we are at the end, get more events from the cache, which then will be used for the next MoveNext call
        if (_currentIndex >= _events.Length)
        {
            GetEventsFromCache(_to);
            _currentIndex = -1;
        }

        _currentIndex++;

        // If the current event at index has a sequence number that is more than the last event sequence number we have, we need to get more events from the cache or event store
        if (_currentIndex < _events.Length)
        {
            if (_currentIndex != 0 && _previousEventSequenceNumber != _events[_currentIndex].Metadata.SequenceNumber + 1)
            {
                GetEventsFromCache(_events[_currentIndex].Metadata.SequenceNumber);
                _currentIndex = 0;
            }

            _previousEventSequenceNumber = _events[_currentIndex].Metadata.SequenceNumber;
        }

        var atEnd = _currentIndex >= _events.Length;
        _logger.LogInformation("At the end? : {AtEnd}", atEnd);

        return !atEnd;
    }

    /// <inheritdoc/>
    public void Refresh(StreamSequenceToken token)
    {
        if (token.IsWarmUp())
        {
            return;
        }

        _logger.LogInformation("Refreshing ({QueueId}) for {MicroserviceId}:{TenantId}:{EventSequenceId} at {CurrentIndex}", _queueId, _microserviceId, _tenantId, _eventSequenceId, _currentIndex);

        if( _events.Any(_ => _.Metadata.SequenceNumber == (ulong)token.SequenceNumber))
        {
            _logger.LogInformation("Refreshing ({QueueId}) for {MicroserviceId}:{TenantId}:{EventSequenceId} at {CurrentIndex} - found event in cursor", _queueId, _microserviceId, _tenantId, _eventSequenceId, _currentIndex);
            return;
        }
        GetEventsFromCache((ulong)token.SequenceNumber);
    }

    /// <inheritdoc/>
    public void RecordDeliveryFailure()
    {
        _logger.LogInformation("RecordDeliveryFailure ({QueueId}) for {MicroserviceId}:{TenantId}:{EventSequenceId} at {EventSequenceNumber}", _queueId, _microserviceId, _tenantId, _eventSequenceId, _events[_currentIndex].Metadata.SequenceNumber);
    }

    void GetEventsFromCache(EventSequenceNumber from)
    {
        _logger.LogInformation("GetEventsFromCache ({QueueId}) for {MicroserviceId}:{TenantId}:{EventSequenceId} at {From}", _queueId, _microserviceId, _tenantId, _eventSequenceId, from);
        _events = _cache.GetView(from).ToArray();

        _logger.LogInformation("Events in cache {Events}", _events.Length);
        if (!_events.Any())
        {
            _cache.Prime(from);
            _events = _cache.GetView(from).ToArray();
        }

        _from = from;
        _currentIndex = -1;
        if (_events.Length > 0)
        {
            _to = _events[^1].Metadata.SequenceNumber;
        }
        else
        {
            _to = EventSequenceNumber.Unavailable;
        }
    }
}
