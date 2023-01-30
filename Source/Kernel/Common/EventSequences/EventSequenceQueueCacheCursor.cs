// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Orleans.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCacheCursor"/> for event sequences.
/// </summary>
public class EventSequenceQueueCacheCursor : IQueueCacheCursor
{
    IEventSequenceCache _cache;
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProvider;
    readonly MicroserviceId _microserviceId;
    readonly TenantId _tenantId;
    readonly EventSequenceId _eventSequenceId;
    AppendedEvent[] _events;
    int _currentIndex;
    EventSequenceNumber _previousEventSequenceNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueCacheCursor"/> class.
    /// </summary>
    /// <param name="cache">The <see cref="IEventSequenceCache"/> for the cursor.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorageProvider"/>.</param>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the cursor is for.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the cursor is for.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the cursor is for.</param>
    /// <param name="from">The from <see cref="EventSequenceNumber"/>.</param>
    public EventSequenceQueueCacheCursor(
        IEventSequenceCache cache,
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProvider,
        MicroserviceId microserviceId,
        TenantId tenantId,
        EventSequenceId eventSequenceId,
        EventSequenceNumber from)
    {
        _cache = cache;
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _microserviceId = microserviceId;
        _tenantId = tenantId;
        _eventSequenceId = eventSequenceId;
        _currentIndex = 0;
        _previousEventSequenceNumber = EventSequenceNumber.Unavailable;
        _events = _cache.GetView(from).ToArray();
        if (!_events.Any())
        {
            PrimeCache(EventSequenceNumber.First);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _events = null!;
        _cache = null!;
    }

    /// <inheritdoc/>
    public IBatchContainer GetCurrent(out Exception exception)
    {
        exception = null!;
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

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (_events.Length == 0)
        {
            PrimeCache(EventSequenceNumber.First);
        }

        if (_events.Length <= _currentIndex)
        {
            return false;
        }

        if (_previousEventSequenceNumber != EventSequenceNumber.Unavailable && _events[_currentIndex].Metadata.SequenceNumber != _previousEventSequenceNumber + 1)
        {
            PrimeCache(_previousEventSequenceNumber + 1);
        }

        _previousEventSequenceNumber = _events[_currentIndex].Metadata.SequenceNumber;

        return ++_currentIndex < _events.Length;
    }

    /// <inheritdoc/>
    public void Refresh(StreamSequenceToken token)
    {
        if (token.IsWarmUp())
        {
            return;
        }
    }

    /// <inheritdoc/>
    public void RecordDeliveryFailure()
    {
    }

    void PrimeCache(EventSequenceNumber start)
    {
        _executionContextManager.Establish(_tenantId, CorrelationId.New(), _microserviceId);
        var end = start + 1000;
        var eventCursor = _eventSequenceStorageProvider().GetRange(_eventSequenceId, start, end).GetAwaiter().GetResult();
        while (eventCursor.MoveNext().GetAwaiter().GetResult())
        {
            foreach (var @event in eventCursor.Current)
            {
                _cache.Add(@event);
            }
        }
        _events = _cache.GetView(start).ToArray();
    }
}
