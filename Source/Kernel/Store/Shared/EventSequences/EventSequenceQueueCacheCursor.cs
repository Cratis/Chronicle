// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.EventSequences.Caching;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCacheCursor"/> for event log regular cache scenario.
/// </summary>
public class EventSequenceQueueCacheCursor : IQueueCacheCursor
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IEventSequenceCache _cache;
    readonly IStreamIdentity _streamIdentity;
    IEventCursor _actualCursor;
    EventSequenceNumber _lastProvidedSequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCacheCursor"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="cache"><see cref="IEventSequenceCache"/>.</param>
    /// <param name="cursorStart">The start of the cursor.</param>
    /// <param name="streamIdentity"><see cref="IStreamIdentity"/> for the stream.</param>
    public EventSequenceQueueCacheCursor(
        IExecutionContextManager executionContextManager,
        IEventSequenceCache cache,
        EventSequenceNumber cursorStart,
        IStreamIdentity streamIdentity)
    {
        _executionContextManager = executionContextManager;
        _cache = cache;
        _streamIdentity = streamIdentity;
        _actualCursor = _cache.GetFrom(cursorStart);
    }

    /// <inheritdoc/>
    public IBatchContainer GetCurrent(out Exception exception)
    {
        exception = null!;
        if (_actualCursor is null)
        {
            return null!;
        }

        var microserviceAndTenant = (MicroserviceAndTenant)_streamIdentity.Namespace;

        var events = Filter(_actualCursor.Current);
        if (!events.Any())
        {
            return null!;
        }

        _lastProvidedSequenceNumber = events.Last().Metadata.SequenceNumber;

        return new EventSequenceBatchContainer(
            events,
            _streamIdentity.Guid,
            microserviceAndTenant.MicroserviceId,
            microserviceAndTenant.TenantId,
            new Dictionary<string, object>
            {
                { RequestContextKeys.MicroserviceId, microserviceAndTenant.MicroserviceId },
                { RequestContextKeys.TenantId, microserviceAndTenant.TenantId }
            });
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
        var microserviceAndTenant = (MicroserviceAndTenant)_streamIdentity.Namespace;
        _executionContextManager.Establish(microserviceAndTenant.TenantId, CorrelationId.New(), microserviceAndTenant.MicroserviceId);

        _actualCursor.Dispose();
        _actualCursor = _cache.GetFrom((ulong)_lastProvidedSequenceNumber);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _actualCursor.Dispose();
        _actualCursor = null!;
    }

    /// <summary>
    /// Filter incoming events.
    /// </summary>
    /// <param name="events">Events to filter.</param>
    /// <returns>Filtered events.</returns>
    protected virtual IEnumerable<AppendedEvent> Filter(IEnumerable<AppendedEvent> events) => events;
}
