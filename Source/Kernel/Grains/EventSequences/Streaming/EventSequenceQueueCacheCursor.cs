// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Orleans.Execution;
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
    AppendedEvent[] _events;
    EventSequenceNumber _to = EventSequenceNumber.Unavailable;
    int _currentIndex;
    EventSequenceNumber _previousEventSequenceNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCacheCursor"/> class.
    /// </summary>
    /// <param name="cache">The <see cref="IEventSequenceCache"/> to use by the cursor.</param>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the cursor is for.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the cursor is for.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the cursor is for.</param>
    /// <param name="from">The from <see cref="EventSequenceNumber"/>.</param>
    public EventSequenceQueueCacheCursor(
        IEventSequenceCache cache,
        MicroserviceId microserviceId,
        TenantId tenantId,
        EventSequenceId eventSequenceId,
        EventSequenceNumber from)
    {
        _cache = cache;
        _microserviceId = microserviceId;
        _tenantId = tenantId;
        _eventSequenceId = eventSequenceId;
        _currentIndex = -1;
        _previousEventSequenceNumber = EventSequenceNumber.Unavailable;
        _events = Array.Empty<AppendedEvent>();

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
            exception = ex;
            return null!;
        }
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (_to == EventSequenceNumber.Unavailable)
        {
            return false;
        }

        _currentIndex++;
        if (_currentIndex >= _events.Length)
        {
            GetEventsFromCache(_previousEventSequenceNumber + 1);
            _currentIndex = 0;
        }

        if (_currentIndex < _events.Length)
        {
            if (_currentIndex != 0 && _events[_currentIndex].Metadata.SequenceNumber != _previousEventSequenceNumber + 1)
            {
                GetEventsFromCache(_previousEventSequenceNumber + 1);
                _currentIndex = 0;
            }

            _previousEventSequenceNumber = _events[_currentIndex].Metadata.SequenceNumber;
        }

        var atEnd = _currentIndex >= _events.Length;
        return !atEnd;
    }

    /// <inheritdoc/>
    public void Refresh(StreamSequenceToken token)
    {
        if (token.IsWarmUp())
        {
            return;
        }

        if (_events.Any(_ => _.Metadata.SequenceNumber == (ulong)token.SequenceNumber))
        {
            return;
        }
        GetEventsFromCache((ulong)token.SequenceNumber);
    }

    /// <inheritdoc/>
    public void RecordDeliveryFailure()
    {
    }

    void GetEventsFromCache(EventSequenceNumber from)
    {
        _events = _cache.GetView(from).ToArray();

        if (!_events.Any())
        {
            _cache.Prime(from);
            _events = _cache.GetView(from).ToArray();
        }

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
