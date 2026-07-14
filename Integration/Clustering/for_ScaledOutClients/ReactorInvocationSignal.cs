// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_ScaledOutClients;

/// <summary>
/// Captures which client instance handled which partition, so scaled-out client specs can assert
/// on the fan out distribution without static fields.
/// </summary>
/// <remarks>
/// Registered as a singleton with the same object reference in both silos, so the reactor running
/// in either client instance and the test code all see the same in-memory state.
/// </remarks>
public class ReactorInvocationSignal
{
    readonly Lock _lock = new();
    readonly List<HandledPartition> _handled = [];

    /// <summary>
    /// Gets a snapshot of everything handled since the last <see cref="Reset"/>.
    /// </summary>
    public IReadOnlyCollection<HandledPartition> Handled
    {
        get
        {
            lock (_lock)
            {
                return [.. _handled];
            }
        }
    }

    /// <summary>
    /// Resets all state so the next test context starts from a clean baseline.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _handled.Clear();
        }
    }

    /// <summary>
    /// Records that a client instance handled an event for a partition.
    /// </summary>
    /// <param name="instance">The parsable silo address identifying the client instance.</param>
    /// <param name="partition">The event source id of the partition.</param>
    public void RecordHandled(string instance, string partition)
    {
        lock (_lock)
        {
            _handled.Add(new HandledPartition(instance, partition));
        }
    }

    /// <summary>
    /// Waits until the given number of invocations have been recorded.
    /// </summary>
    /// <param name="expected">The number of invocations to wait for.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expected count is not reached within the timeout.</exception>
    public async Task WaitForHandledCount(int expected, TimeSpan timeout)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout);
        while (Handled.Count < expected)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                throw new InvalidOperationException($"Expected {expected} handled events within the timeout, but only got {Handled.Count}.");
            }

            await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}
