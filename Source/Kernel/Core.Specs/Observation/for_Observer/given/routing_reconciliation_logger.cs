// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// Signals the exact point where Routing has reconciled the partition holds.
/// </summary>
public class routing_reconciliation_logger : ILogger
{
    readonly TaskCompletionSource _reconciled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Waits for reconciliation, failing rather than silently proceeding if it never happens.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task WaitUntilReconciled() => _reconciled.Task.WaitAsync(TimeSpan.FromSeconds(10));

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (string.Equals(eventId.Name, "ReleasedPartitionHolds", StringComparison.Ordinal) ||
            string.Equals(eventId.Name, "KeptOwnedPartitionHolds", StringComparison.Ordinal))
        {
            _reconciled.TrySetResult();
        }
    }
}
