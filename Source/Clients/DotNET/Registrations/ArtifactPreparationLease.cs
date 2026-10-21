// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// A managed asynchronous lease with no wait handles or public store disposal contract.
/// </summary>
internal sealed class ArtifactPreparationLease
{
    readonly object _lock = new();
    readonly Queue<TaskCompletionSource> _waiting = new();
    bool _held;

    /// <summary>
    /// Acquires the lease or waits on a queued completion signal without holding a monitor across an await.
    /// </summary>
    /// <returns>The acquisition task.</returns>
    internal Task WaitAsync()
    {
        lock (_lock)
        {
            if (!_held)
            {
                _held = true;
                return Task.CompletedTask;
            }

            var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiting.Enqueue(waiter);
            return waiter.Task;
        }
    }

    /// <summary>
    /// Hands ownership to the next queued operation or releases it when none is waiting.
    /// </summary>
    /// <exception cref="ArtifactPreparationReentered">There is no held lease to release.</exception>
    internal void Release()
    {
        TaskCompletionSource? waiter;
        lock (_lock)
        {
            if (!_held) throw new ArtifactPreparationReentered();
            if (!_waiting.TryDequeue(out waiter)) _held = false;
        }

        waiter?.SetResult();
    }
}
