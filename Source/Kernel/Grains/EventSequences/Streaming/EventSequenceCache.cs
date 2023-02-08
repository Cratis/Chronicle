// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

#pragma warning disable CA1051 // Do not declare visible instance fields

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCache"/>.
/// </summary>
public class EventSequenceCache : IEventSequenceCache
{
    /// <summary>
    /// The maximum number of events to keep in the cache.
    /// </summary>
    public const int MaxNumberOfEvents = 10000;

    /// <summary>
    /// The pressure point for the cache.
    /// </summary>
    public const int PressurePoint = 9000;

    /// <summary>
    /// Number of events to purge when the cache is under pressure.
    /// </summary>
    public const int NumberOfEventsToPurge = 2000;

    /// <summary>
    /// The number of events to fetch from the event store.
    /// </summary>
    public const int NumberOfEventsToFetch = 1000;

#pragma warning disable SA1600, MA0016 // Elements should be documented + concrete type should not be used
    protected readonly LinkedList<AppendedEvent> _events;
    protected readonly Dictionary<EventSequenceNumber, LinkedListNode<AppendedEvent>> _eventsBySequenceNumber;
#pragma warning restore SA1600

    readonly object _lock = new();
    readonly MicroserviceId _microserviceId;
    readonly TenantId _tenantId;
    readonly EventSequenceId _eventSequenceId;
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProvider;
    readonly ILogger<EventSequenceCache> _logger;

    /// <inheritdoc/>
    public int Count => _events.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCache"/> class.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the cache is for.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the cache is for.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the cache is for.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorageProvider"/> for working with the event store.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public EventSequenceCache(
        MicroserviceId microserviceId,
        TenantId tenantId,
        EventSequenceId eventSequenceId,
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProvider,
        ILogger<EventSequenceCache> logger)
    {
        _events = new();
        _eventsBySequenceNumber = new();
        _microserviceId = microserviceId;
        _tenantId = tenantId;
        _eventSequenceId = eventSequenceId;
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _events.Clear();
        _eventsBySequenceNumber.Clear();
    }

    /// <inheritdoc/>
    public void Add(AppendedEvent @event)
    {
        lock (_lock)
        {
            if (_eventsBySequenceNumber.ContainsKey(@event.Metadata.SequenceNumber)) return;
            if (_events.Count > 0 && _events.Last!.Value.Metadata.SequenceNumber >= @event.Metadata.SequenceNumber)
            {
                throw new EventSequenceNumberIsLessThanLastEventInCache(@event.Metadata.SequenceNumber, _events.Last!.Value.Metadata.SequenceNumber);
            }
            if (_events.Count > 0 && _events.Last!.Value.Metadata.SequenceNumber + 1 < @event.Metadata.SequenceNumber)
            {
                Prime(_events.Last!.Value.Metadata.SequenceNumber + 1);
            }

            AddImplementation(@event);
        }
    }

    /// <inheritdoc/>
    public void Prime(EventSequenceNumber from)
    {
        var to = from + NumberOfEventsToFetch;
        _logger.Priming(from, to);
        _executionContextManager.Establish(_tenantId, CorrelationId.New(), _microserviceId);
        var eventCursor = _eventSequenceStorageProvider().GetRange(_eventSequenceId, from, to).GetAwaiter().GetResult();

        lock (_lock)
        {
            while (eventCursor.MoveNext().GetAwaiter().GetResult())
            {
                foreach (var @event in eventCursor.Current)
                {
                    AddImplementation(@event);
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool IsUnderPressure() => _events.Count > PressurePoint;

    /// <inheritdoc/>
    public void Purge()
    {
        if (IsUnderPressure())
        {
            lock (_lock)
            {
                foreach (var @event in _events.Take(NumberOfEventsToPurge).ToArray())
                {
                    _events.Remove(@event);
                    _eventsBySequenceNumber.Remove(@event.Metadata.SequenceNumber);
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool HasEvent(EventSequenceNumber sequenceNumber) => _eventsBySequenceNumber.ContainsKey(sequenceNumber);

    /// <inheritdoc/>
    public LinkedListNode<AppendedEvent>? GetEvent(EventSequenceNumber sequenceNumber)
    {
        lock (_lock)
        {
            if (HasEvent(sequenceNumber))
            {
                return _eventsBySequenceNumber[sequenceNumber];
            }

            return null;
        }
    }

    void AddImplementation(AppendedEvent @event) => _eventsBySequenceNumber[@event.Metadata.SequenceNumber] = _events.AddLast(@event);
}
