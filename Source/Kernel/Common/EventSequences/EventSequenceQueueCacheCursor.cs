// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Orleans.Execution;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCacheCursor"/> for event log regular cache scenario.
/// </summary>
public class EventSequenceQueueCacheCursor : IQueueCacheCursor
{
#pragma warning disable SA1202, CA1051
    /// <summary>
    /// Gets the <see cref="IEventSequenceStorageProvider"/>.
    /// </summary>
    protected readonly IEventSequenceStorageProvider _storageProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly EventSequenceNumber _cursorStart;
    readonly IStreamIdentity _streamIdentity;
    IEventCursor? _actualCursor;
    EventSequenceNumber _lastProvidedSequenceNumber = EventSequenceNumber.First;
    bool _firstRun;

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
        _cursorStart = cursorStart;
        _streamIdentity = streamIdentity;
        _storageProvider = storageProvider;
        _firstRun = true;
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
    public bool MoveNext()
    {
        InitializeCursorOnFirstRun();
        return _actualCursor?.MoveNext().GetAwaiter().GetResult() ?? false;
    }

    /// <inheritdoc/>
    public void RecordDeliveryFailure()
    {
    }

    /// <inheritdoc/>
    public void Refresh(StreamSequenceToken token)
    {
        var microserviceAndTenant = (MicroserviceAndTenant)_streamIdentity.Namespace;
        _executionContextManager.Establish(microserviceAndTenant.TenantId, CorrelationId.New(), microserviceAndTenant.MicroserviceId);

        _actualCursor?.Dispose();
        _actualCursor = GetActualEventCursor(_streamIdentity.Guid, (ulong)_lastProvidedSequenceNumber).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _actualCursor?.Dispose();
        _actualCursor = null!;
    }

    void InitializeCursorOnFirstRun()
    {
        if (_firstRun)
        {
            _actualCursor = GetActualEventCursor(_streamIdentity.Guid, _cursorStart).GetAwaiter().GetResult();
            _firstRun = false;
        }
    }

    /// <summary>
    /// Get the actual event cursor.
    /// </summary>
    /// <param name="sequenceId">The event sequence to get for.</param>
    /// <param name="sequenceNumber">The start sequence number.</param>
    /// <returns>Actual event cursor.</returns>
    protected virtual Task<IEventCursor> GetActualEventCursor(EventSequenceId sequenceId, EventSequenceNumber sequenceNumber) =>
        _storageProvider.GetFromSequenceNumber(sequenceId, sequenceNumber);
}
