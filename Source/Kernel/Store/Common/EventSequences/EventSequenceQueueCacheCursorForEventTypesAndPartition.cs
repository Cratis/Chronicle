// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCacheCursor"/> for event log filtered for event types and partition.
/// </summary>
public class EventSequenceQueueCacheCursorForEventTypesAndPartition : IQueueCacheCursor
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IEventSequenceStorageProvider _eventLogStorageProvider;
    readonly IStreamIdentity _streamIdentity;
    readonly IEnumerable<EventType> _eventTypes;
    readonly EventSourceId? _partition;
    IEventCursor? _cursor;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCacheCursorForEventTypesAndPartition"/>.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="eventLogStorageProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    /// <param name="streamIdentity"><see cref="IStreamIdentity"/> for the stream.</param>
    /// <param name="token"><see cref="StreamSequenceToken"/> that represents the starting point to get from.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType">Event types</see> to filter the cursor with - default all.</param>
    /// <param name="partition">Optional <see cref="EventSourceId"/> partition to filter for.</param>
    public EventSequenceQueueCacheCursorForEventTypesAndPartition(
        IExecutionContextManager executionContextManager,
        IEventSequenceStorageProvider eventLogStorageProvider,
        IStreamIdentity streamIdentity,
        StreamSequenceToken token,
        IEnumerable<EventType>? eventTypes = default,
        EventSourceId? partition = default)
    {
        _executionContextManager = executionContextManager;
        _eventLogStorageProvider = eventLogStorageProvider;
        _streamIdentity = streamIdentity;
        _eventTypes = eventTypes ?? Array.Empty<EventType>();
        _partition = partition;

        FindEventsFrom(token);
    }

    /// <inheritdoc/>
    public IBatchContainer GetCurrent(out Exception exception)
    {
        exception = null!;
        if (_cursor == null) return null!;

        try
        {
            var appendedEvents = _cursor.Current.ToArray();
            if (appendedEvents.Length == 0)
            {
                return null!;
            }

            var microserviceAndTenant = (MicroserviceAndTenant)_streamIdentity.Namespace;
            return new EventSequenceBatchContainer(
                appendedEvents,
                _streamIdentity.Guid,
                microserviceAndTenant.MicroserviceId,
                microserviceAndTenant.TenantId,
                new Dictionary<string, object>
                {
                    { RequestContextKeys.MicroserviceId, microserviceAndTenant.MicroserviceId },
                    { RequestContextKeys.TenantId, microserviceAndTenant.TenantId }
                });
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        return null!;
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (_cursor is null) return false;

        return _cursor.MoveNext().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public void RecordDeliveryFailure()
    {
    }

    /// <inheritdoc/>
    public void Refresh(StreamSequenceToken token)
    {
        FindEventsFrom(token);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cursor?.Dispose();
        _cursor = null!;
    }

    void FindEventsFrom(StreamSequenceToken token)
    {
        // When the sequence number represents a warm up event. We don't want to go to the event store to get events.
        if (token.IsWarmUp())
        {
            return;
        }

        var microserviceAndTenant = (MicroserviceAndTenant)_streamIdentity.Namespace;
        _executionContextManager.Establish(microserviceAndTenant.TenantId, CorrelationId.New(), microserviceAndTenant.MicroserviceId);
        _cursor = _eventLogStorageProvider.GetFromSequenceNumber(_streamIdentity.Guid, (ulong)token.SequenceNumber, _partition, _eventTypes).GetAwaiter().GetResult();
    }
}
