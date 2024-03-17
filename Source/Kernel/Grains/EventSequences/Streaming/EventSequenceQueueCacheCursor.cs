// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Kernel.EventSequences;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueCacheCursor"/> for event sequences.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceQueueCacheCursor"/> class.
/// </remarks>
/// <param name="cache">The <see cref="IEventSequenceCache"/> to use by the cursor.</param>
/// <param name="microserviceId">The <see cref="MicroserviceId"/> the cursor is for.</param>
/// <param name="tenantId">The <see cref="TenantId"/> the cursor is for.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the cursor is for.</param>
/// <param name="from">The from <see cref="EventSequenceNumber"/>.</param>
public class EventSequenceQueueCacheCursor(
    IEventSequenceCache cache,
    MicroserviceId microserviceId,
    TenantId tenantId,
    EventSequenceId eventSequenceId,
    EventSequenceNumber from) : IQueueCacheCursor
{
    CachedAppendedEvent? _current;
    EventSequenceNumber _currentSequenceNumber = from;

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
                new MicroserviceAndTenant(microserviceId, tenantId),
                eventSequenceId.Value.ToString());

            return new EventSequenceBatchContainer(
                new[] { @event },
                streamId,
                new Dictionary<string, object>());
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

        if (!cache.HasEvent((ulong)token.SequenceNumber))
        {
            cache.Prime((ulong)token.SequenceNumber);
        }
    }

    /// <inheritdoc/>
    public void RecordDeliveryFailure()
    {
    }

    CachedAppendedEvent? TryToGetFromCache(EventSequenceNumber sequenceNumber)
    {
        if (!cache.HasEvent(sequenceNumber))
        {
            cache.Prime(sequenceNumber);
        }

        if (cache.HasEvent(sequenceNumber))
        {
            return cache.GetEvent(sequenceNumber);
        }

        return null;
    }
}
