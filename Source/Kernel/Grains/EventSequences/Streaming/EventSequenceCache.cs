// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

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
    public const int NumberOfEventsToFetch = 100;

#pragma warning disable CA1051, SA1600, MA0016 // Elements should be documented + concrete type should not be used + visible instance fields.
    protected readonly Dictionary<EventSequenceNumber, CachedAppendedEvent> _eventsBySequenceNumber = new();
    protected CachedAppendedEvent? _head;
    protected CachedAppendedEvent? _tail;
#pragma warning restore SA1600

    readonly object _lock = new();
    readonly IEventSequenceStorage _eventSequenceStorage;
    readonly ILogger<EventSequenceCache> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCache"/> class.
    /// </summary>
    /// <param name="eventSequenceStorage">Provider for <see cref="IEventSequenceStorage"/> for working with the event store.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public EventSequenceCache(
        IEventSequenceStorage eventSequenceStorage,
        ILogger<EventSequenceCache> logger)
    {
        _eventsBySequenceNumber = new();
        _eventSequenceStorage = eventSequenceStorage;
        _logger = logger;
    }

    /// <inheritdoc/>
    public int Count => _eventsBySequenceNumber.Count;

    /// <inheritdoc/>
    public EventSequenceNumber Head => _head?.Event.Metadata.SequenceNumber ?? EventSequenceNumber.Unavailable;

    /// <inheritdoc/>
    public EventSequenceNumber Tail => _tail?.Event.Metadata.SequenceNumber ?? EventSequenceNumber.Unavailable;

    /// <inheritdoc/>
    public void Dispose()
    {
        _eventsBySequenceNumber.Clear();
        _head = null;
        _tail = null;
    }

    /// <inheritdoc/>
    public void Add(AppendedEvent @event)
    {
        lock (_lock)
        {
            AddImplementation(@event);
        }
    }

    /// <inheritdoc/>
    public void Prime(EventSequenceNumber from)
    {
        if (from < (_head?.Event.Metadata.SequenceNumber ?? EventSequenceNumber.First))
        {
            return;
        }

        var to = from + NumberOfEventsToFetch;
        _logger.Priming(from, to);

        var eventCursor = _eventSequenceStorage.GetRange(from, to).GetAwaiter().GetResult();

        lock (_lock)
        {
            var populateTask = Task.Run(async () =>
            {
                while (await eventCursor.MoveNext())
                {
                    foreach (var @event in eventCursor.Current)
                    {
                        AddImplementation(@event);
                    }
                }
            });

            populateTask.Wait();
        }
    }

    /// <inheritdoc/>
    public bool IsUnderPressure() => Count > PressurePoint;

    /// <inheritdoc/>
    public void Purge()
    {
        if (IsUnderPressure())
        {
            RelievePressure();
        }
    }

    /// <inheritdoc/>
    public bool HasEvent(EventSequenceNumber sequenceNumber) => _eventsBySequenceNumber.ContainsKey(sequenceNumber);

    /// <inheritdoc/>
    public CachedAppendedEvent? GetEvent(EventSequenceNumber sequenceNumber)
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

    /// <inheritdoc/>
    public async Task PrimeWithTailWindow()
    {
        var tail = await _eventSequenceStorage.GetTailSequenceNumber();
        tail -= NumberOfEventsToFetch;
        if ((long)tail.Value < 0) tail = 0;
        Prime(tail);
    }

    void AddImplementation(AppendedEvent @event)
    {
        if (_eventsBySequenceNumber.ContainsKey(@event.Metadata.SequenceNumber))
        {
            return;
        }

        if (_head is null)
        {
            _head = new CachedAppendedEvent(@event);
            _tail = _head;
            _eventsBySequenceNumber[@event.Metadata.SequenceNumber] = _head;
            return;
        }

        if (@event.Metadata.SequenceNumber > _tail!.Event.Metadata.SequenceNumber)
        {
            _tail.Next = new CachedAppendedEvent(@event, null);
            _tail = _tail.Next;
            _eventsBySequenceNumber[@event.Metadata.SequenceNumber] = _tail;
            return;
        }

        throw new EventSequenceNumberIsLessThanLastEventInCache(@event.Metadata.SequenceNumber, _tail.Event.Metadata.SequenceNumber);
    }

    void RelievePressure()
    {
        lock (_lock)
        {
            while (Count > MaxNumberOfEvents - NumberOfEventsToPurge)
            {
                _eventsBySequenceNumber.Remove(_head!.Event.Metadata.SequenceNumber);
                _head = _head.Next;
            }
        }
    }
}
