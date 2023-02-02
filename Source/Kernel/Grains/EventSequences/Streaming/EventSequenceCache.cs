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
    /// The number of events to fetch from the event store.
    /// </summary>
    public const int NumberOfEventsToFetch = 1000;

#pragma warning disable SA1600 // Elements should be documented
    protected readonly SortedSet<AppendedEvent> _events;
    protected readonly SortedSet<AppendedEventByDate> _eventsByDate;
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
        _events = new(new AppendedEventComparer());
        _eventsByDate = new(new AppendedEventByDateComparer());
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
        _eventsByDate.Clear();
    }

    /// <inheritdoc/>
    public void Add(AppendedEvent @event)
    {
        lock (_lock)
        {
            if (_events.Contains(@event))
            {
                _eventsByDate.RemoveWhere(_ => _.Event == @event);
                _eventsByDate.Add(new(@event, DateTimeOffset.UtcNow));
                return;
            }

            _events.Add(@event);
            _eventsByDate.Add(new(@event, DateTimeOffset.UtcNow));
        }
    }

    /// <inheritdoc/>
    public SortedSet<AppendedEvent> GetView(EventSequenceNumber from, EventSequenceNumber? to = null)
    {
        lock (_lock)
        {
            var fromEvent = AppendedEvent.EmptyWithEventSequenceNumber(from);
            var toEvent = AppendedEvent.EmptyWithEventSequenceNumber(to ?? EventSequenceNumber.Max);
            return _events.GetViewBetween(fromEvent, toEvent);
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
                    {
                        Add(@event);
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool IsUnderPressure()
    {
        return _events.Count > MaxNumberOfEvents;
    }

    /// <inheritdoc/>
    public void Purge()
    {
        if (IsUnderPressure())
        {
            lock (_lock)
            {
                foreach (var @event in _eventsByDate.Take(_events.Count - MaxNumberOfEvents).ToList())
                {
                    _events.Remove(@event.Event);
                    _eventsByDate.Remove(@event);
                }
            }
        }
    }
}
