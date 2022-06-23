// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCacheCursor"/> for event log filtered for event types.
/// </summary>
public class EventSequenceQueueCacheCursorForEventTypes : IQueueCacheCursor
{
    readonly IEventCursor _actualCursor;
    readonly IStreamIdentity _streamIdentity;
    readonly IEnumerable<EventType> _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCacheCursor"/> class.
    /// </summary>
    /// <param name="actualCursor">The actual <see cref="IEventCursor"/>.</param>
    /// <param name="streamIdentity"><see cref="IStreamIdentity"/> for the stream.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType">Event types</see> to filter the cursor with - default all.</param>
    public EventSequenceQueueCacheCursorForEventTypes(
        IEventCursor actualCursor,
        IStreamIdentity streamIdentity,
        IEnumerable<EventType> eventTypes)
    {
        _actualCursor = actualCursor;
        _streamIdentity = streamIdentity;
        _eventTypes = eventTypes;
    }

    /// <inheritdoc/>
    public IBatchContainer GetCurrent(out Exception exception)
    {
        exception = null!;

        var microserviceAndTenant = (MicroserviceAndTenant)_streamIdentity.Namespace;
        var events = _actualCursor.Current.Where(_ => _eventTypes.Any(et => et.Id == _.Metadata.Type.Id)).ToArray();
        if (events.Length == 0)
        {
            return null!;
        }

        return new EventSequenceBatchContainer(
            events,
            _streamIdentity.Guid,
            microserviceAndTenant.MicroserviceId,
            microserviceAndTenant.TenantId,
            new Dictionary<string, object> { { RequestContextKeys.TenantId, _streamIdentity.Namespace } });
    }

    /// <inheritdoc/>
    public bool MoveNext() => _actualCursor.MoveNext().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public void RecordDeliveryFailure()
    {
    }

    /// <inheritdoc/>
    public void Refresh(StreamSequenceToken token)
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
