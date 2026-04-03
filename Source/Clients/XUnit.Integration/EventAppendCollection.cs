// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents an implementation of <see cref="IEventAppendCollection"/>.
/// </summary>
public class EventAppendCollection : IEventAppendCollection, IObserveEventAppended
{
    readonly List<IObservableEventSequence> _subscribed = [];
    readonly List<CollectedEvent> _collected = [];
    readonly SemaphoreSlim _signal = new(0);
    readonly object _lock = new();
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventAppendCollection"/> class.
    /// </summary>
    /// <param name="eventSequences">The event sequences to observe.</param>
    public EventAppendCollection(params IEventSequence[] eventSequences)
    {
        foreach (var observable in eventSequences.OfType<IObservableEventSequence>())
        {
            observable.Subscribe(this);
            _subscribed.Add(observable);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<CollectedEvent> All
    {
        get
        {
            lock (_lock)
                return [.. _collected];
        }
    }

    /// <inheritdoc/>
    public CollectedEvent Last
    {
        get
        {
            lock (_lock)
            {
                return _collected.Count > 0
                    ? _collected[^1]
                    : throw new InvalidOperationException("No events have been collected.");
            }
        }
    }

    /// <inheritdoc/>
    public void OnAppend(CorrelationId correlationId, EventSourceId eventSourceId, object @event, IImmutableList<Causation> causationChain, AppendResult result)
    {
        if (_disposed) return;

        var collectedEvent = new CollectedEvent
        {
            CorrelationId = correlationId,
            EventSourceId = eventSourceId,
            SequenceNumber = result.IsSuccess ? result.SequenceNumber : EventSequenceNumber.Unavailable,
            Event = @event,
            CausationChain = causationChain,
            ConstraintViolations = result.ConstraintViolations,
            ConcurrencyViolations = result.ConcurrencyViolation is not null ? [result.ConcurrencyViolation] : [],
            Errors = result.Errors
        };

        lock (_lock)
            _collected.Add(collectedEvent);

        _signal.Release();
    }

    /// <inheritdoc/>
    public void OnAppendMany(CorrelationId correlationId, EventSourceId eventSourceId, IEnumerable<object> events, IImmutableList<Causation> causationChain, AppendManyResult result)
    {
        if (_disposed) return;

        var eventsList = events.ToList();
        var sequenceNumbers = result.SequenceNumbers.ToList();

        lock (_lock)
        {
            for (var i = 0; i < eventsList.Count; i++)
            {
                var sequenceNumber = result.IsSuccess && i < sequenceNumbers.Count
                    ? sequenceNumbers[i]
                    : EventSequenceNumber.Unavailable;

                _collected.Add(new CollectedEvent
                {
                    CorrelationId = correlationId,
                    EventSourceId = eventSourceId,
                    SequenceNumber = sequenceNumber,
                    Event = eventsList[i],
                    CausationChain = causationChain,
                    ConstraintViolations = result.ConstraintViolations,
                    ConcurrencyViolations = result.ConcurrencyViolations,
                    Errors = result.Errors
                });
            }

            _signal.Release(eventsList.Count);
        }
    }

    /// <inheritdoc/>
    public async Task WaitForCount(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);

        while (true)
        {
            lock (_lock)
            {
                if (_collected.Count >= count)
                    return;
            }

            try
            {
                await _signal.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Timed out waiting for {count} collected events. Collected {_collected.Count} so far.");
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var seq in _subscribed)
            seq.Unsubscribe(this);

        _signal.Dispose();
    }
}
