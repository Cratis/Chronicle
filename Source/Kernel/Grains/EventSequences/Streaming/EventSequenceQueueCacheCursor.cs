// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Orleans.Execution;
using Orleans.Runtime;
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
    CachedAppendedEvent? _current;
    EventSequenceNumber _currentSequenceNumber;

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
        _currentSequenceNumber = from;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public IBatchContainer GetCurrent(out Exception exception)
    {
        exception = null!;
        try
        {
            if (_current is null)
            {
                return null!;
            }

            var @event = _current.Event;
            var streamId = StreamId.Create(
                new MicroserviceAndTenant(_microserviceId, _tenantId),
                _eventSequenceId.Value.ToString());

            return new EventSequenceBatchContainer(
                new[] { @event },
                streamId,
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
        if (_current is null)
        {
            _current = TryToGetFromCache(_currentSequenceNumber);
        }
        else
        {
            _current = _current.Next ?? TryToGetFromCache(_currentSequenceNumber.Next());
        }

        if (_current is not null)
        {
            _currentSequenceNumber = _current.Event.Metadata.SequenceNumber;
        }

        return _current is not null;
    }

    /// <inheritdoc/>
    public void Refresh(StreamSequenceToken token)
    {
        if (token.IsWarmUp())
        {
            return;
        }

        if (!_cache.HasEvent((ulong)token.SequenceNumber))
        {
            _cache.Prime((ulong)token.SequenceNumber);
        }
    }

    /// <inheritdoc/>
    public void RecordDeliveryFailure()
    {
    }

    CachedAppendedEvent? TryToGetFromCache(EventSequenceNumber sequenceNumber)
    {
        if (!_cache.HasEvent(sequenceNumber))
        {
            _cache.Prime(sequenceNumber);
        }

        if (_cache.HasEvent(sequenceNumber))
        {
            return _cache.GetEvent(sequenceNumber);
        }

        return null;
    }
}
