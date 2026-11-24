// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing;

/// <summary>
/// Records what the served reactor and reducer received, in the order they received it.
/// </summary>
public class ServedSignal
{
    readonly Lock _lock = new();
    readonly List<ServedDelivery> _reacted = [];
    readonly List<ServedDelivery> _reduced = [];

    /// <summary>
    /// Gets what the reactor received since the last <see cref="Reset"/>.
    /// </summary>
    public IReadOnlyList<ServedDelivery> Reacted
    {
        get
        {
            lock (_lock)
            {
                return [.. _reacted];
            }
        }
    }

    /// <summary>
    /// Gets what the reducer received since the last <see cref="Reset"/>.
    /// </summary>
    public IReadOnlyList<ServedDelivery> Reduced
    {
        get
        {
            lock (_lock)
            {
                return [.. _reduced];
            }
        }
    }

    /// <summary>
    /// Clears everything recorded.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _reacted.Clear();
            _reduced.Clear();
        }
    }

    /// <summary>
    /// Records a delivery to the reactor.
    /// </summary>
    /// <param name="instance">The client instance that received it.</param>
    /// <param name="source">The event source id.</param>
    /// <param name="sequence">The sequence carried by the event.</param>
    public void RecordReacted(string instance, string source, int sequence)
    {
        lock (_lock)
        {
            _reacted.Add(new(instance, source, sequence));
        }
    }

    /// <summary>
    /// Records a delivery to the reducer.
    /// </summary>
    /// <param name="instance">The client instance that received it.</param>
    /// <param name="source">The event source id.</param>
    /// <param name="sequence">The sequence carried by the event.</param>
    public void RecordReduced(string instance, string source, int sequence)
    {
        lock (_lock)
        {
            _reduced.Add(new(instance, source, sequence));
        }
    }

    /// <summary>
    /// Waits until the reactor and the reducer have each received the given number of events.
    /// </summary>
    /// <param name="expected">The number of events each must have received.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the count is not reached in time.</exception>
    public async Task WaitForDeliveries(int expected, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (Reacted.Count < expected || Reduced.Count < expected)
        {
            if (cancellation.IsCancellationRequested)
            {
                throw new InvalidOperationException($"Expected {expected} deliveries to the reactor and the reducer, saw {Reacted.Count} and {Reduced.Count}.");
            }

            // Task.Delay: poll interval while waiting for the deliveries, bounded by the cancellation.
            await Task.Delay(100, cancellation.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}
