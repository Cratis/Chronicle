// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Defines a scoped collection of <see cref="CollectedEvent"/> entries captured during a test operation.
/// </summary>
/// <remarks>
/// Start a collection scope with <c>StartCollectingAppends()</c> on the test fixture, then append events. All
/// appends to any subscribed event sequence are captured automatically. Dispose the collection to
/// stop capturing. Inspect <see cref="All"/> or <see cref="Last"/> in your assertions.
/// </remarks>
public interface IEventAppendCollection : IDisposable
{
    /// <summary>
    /// Gets a snapshot of all events collected so far.
    /// </summary>
    IReadOnlyList<CollectedEvent> All { get; }

    /// <summary>
    /// Gets the most recently collected event.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no events have been collected yet.</exception>
    CollectedEvent Last { get; }

    /// <summary>
    /// Waits until at least <paramref name="count"/> events have been collected.
    /// </summary>
    /// <param name="count">The minimum number of events to wait for.</param>
    /// <param name="timeout">Optional timeout. Defaults to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">Thrown when the timeout is exceeded before enough events are collected.</exception>
    Task WaitForCount(int count, TimeSpan? timeout = default);
}
