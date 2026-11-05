// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Bounds how many grain storage operations run against the database at once, so a burst waits in memory.
/// </summary>
/// <remarks>
/// A silo restarted after being down for a while has every overdue reminder fire at once, and each activation reads its state.
/// Thousands of concurrent reads overflow the driver's connection pool and its wait queue, the reads fail, the reminders fire
/// again and the load never drains (#5677). Waiting here costs a little memory and no connection.
/// </remarks>
/// <param name="maxConcurrentOperations">The most operations that run at once; zero or less means no bound.</param>
public sealed class StorageConcurrencyGate(int maxConcurrentOperations) : IDisposable
{
    readonly SemaphoreSlim _gate = new(maxConcurrentOperations > 0 ? maxConcurrentOperations : int.MaxValue);

    /// <inheritdoc/>
    public void Dispose() => _gate.Dispose();

    /// <summary>
    /// Runs an operation once there is room for it.
    /// </summary>
    /// <param name="operation">The operation to run.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Run(Func<Task> operation)
    {
        await _gate.WaitAsync();
        try
        {
            await operation();
        }
        finally
        {
            _gate.Release();
        }
    }
}
