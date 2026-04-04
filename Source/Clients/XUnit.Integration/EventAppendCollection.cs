// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents an implementation of <see cref="IEventAppendCollection"/>.
/// </summary>
public class EventAppendCollection : IEventAppendCollection
{
    readonly List<IDisposable> _subscriptions = [];
    readonly List<AppendedEventWithResult> _collected = [];
    readonly SemaphoreSlim _signal = new(0);
    readonly object _lock = new();
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventAppendCollection"/> class.
    /// </summary>
    /// <param name="eventSequences">The event sequences to observe.</param>
    public EventAppendCollection(params IEventSequence[] eventSequences)
    {
        foreach (var seq in eventSequences)
        {
            _subscriptions.Add(seq.AppendOperations.Subscribe(OnAppendOperations));
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<AppendedEventWithResult> All
    {
        get
        {
            lock (_lock)
                return [.. _collected];
        }
    }

    /// <inheritdoc/>
    public AppendedEventWithResult Last
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

        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _signal.Dispose();
    }

    void OnAppendOperations(IEnumerable<AppendedEventWithResult> operations)
    {
        if (_disposed) return;

        var items = operations.ToList();

        lock (_lock)
        {
            _collected.AddRange(items);
            _signal.Release(items.Count);
        }
    }
}
