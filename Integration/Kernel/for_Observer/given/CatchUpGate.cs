// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

/// <summary>
/// Holds the delivery of events to the reactors of the catch-up cap specifications until it is released.
/// </summary>
public class CatchUpGate
{
    TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    volatile bool _closed;

    /// <summary>
    /// Gets the deliveries that reached the gate, as the name of the reactor and the content of the event.
    /// </summary>
    public ConcurrentQueue<(string Reactor, string Content)> Deliveries { get; } = new();

    /// <summary>
    /// Clears the deliveries and opens the gate.
    /// </summary>
    public void Reset()
    {
        Deliveries.Clear();
        _release.TrySetResult();
        _closed = false;
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Makes deliveries wait at the gate.
    /// </summary>
    public void Close() => _closed = true;

    /// <summary>
    /// Lets the waiting deliveries through and stops holding new ones.
    /// </summary>
    public void Open()
    {
        _closed = false;
        _release.TrySetResult();
    }

    /// <summary>
    /// Records a delivery and waits for the gate to open when it is closed.
    /// </summary>
    /// <param name="reactor">The name of the delivering reactor.</param>
    /// <param name="content">The content of the event.</param>
    /// <returns>A task that completes when the delivery may proceed.</returns>
    public async Task Deliver(string reactor, string content)
    {
        Deliveries.Enqueue((reactor, content));
        if (_closed)
        {
            await _release.Task.WaitAsync(TimeSpan.FromSeconds(120));
        }
    }
}
