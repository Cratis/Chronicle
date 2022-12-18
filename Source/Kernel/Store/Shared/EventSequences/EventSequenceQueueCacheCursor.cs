// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    readonly IStreamIdentity _streamIdentity;
    protected readonly IEventSequenceStorageProvider _storageProvider;
    IEventCursor _actualCursor;
    EventSequenceNumber _lastProvidedSequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCacheCursor"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="cursorStart">The start of the cursor.</param>
    /// <param name="streamIdentity"><see cref="IStreamIdentity"/> for the stream.</param>
    /// <param name="storageProvider"><see cref="IEventSequenceStorageProvider"/> to ue for getting events from sequence.</param>
    public EventSequenceQueueCacheCursor(
        IExecutionContextManager executionContextManager,
        EventSequenceNumber cursorStart,
        IStreamIdentity streamIdentity,
        IEventSequenceStorageProvider storageProvider)
    {
        _executionContextManager = executionContextManager;
        _streamIdentity = streamIdentity;
        _storageProvider = storageProvider;
        _actualCursor = GetActualEventCursor(_streamIdentity.Guid, cursorStart).GetAwaiter().GetResult();
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

        var events = _actualCursor.Current;
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
        _actualCursor = GetActualEventCursor(_streamIdentity.Guid, (ulong)_lastProvidedSequenceNumber).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _actualCursor.Dispose();
        _actualCursor = null!;
    }

    /// <summary>
    /// Get the actual event cursor.
    /// </summary>
    /// <param name="sequenceId">The event sequence to get for.</param>
    /// <param name="sequenceNumber">The start </param>
    /// <returns>Actual event cursor.</returns>
    protected virtual Task<IEventCursor> GetActualEventCursor(EventSequenceId sequenceId, EventSequenceNumber sequenceNumber) => null!;
}
