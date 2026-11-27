// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// An asynchronous mutual-exclusion gate that can never be disposed out from under a holder or a waiter.
/// </summary>
/// <remarks>
/// A grain is disposed by the runtime when its deactivation ends - or when the runtime stops waiting for a deactivation
/// that outlived its timeout - while a turn it does not wait for can still hold the gate or be queued on it. Disposing a
/// <see cref="SemaphoreSlim"/> in that moment makes the holder's release throw and leaves every waiter queued forever.
/// This gate holds no unmanaged resource, so it has nothing to dispose; waiters are admitted in the order they asked.
/// </remarks>
internal sealed class AsyncGate
{
    readonly Lock _lock = new();
    readonly Queue<TaskCompletionSource> _waiters = new();
    bool _held;

    /// <summary>
    /// Gets whether a caller currently holds the gate.
    /// </summary>
    public bool IsHeld
    {
        get
        {
            lock (_lock) return _held;
        }
    }

    /// <summary>
    /// Waits until the gate is free and takes it.
    /// </summary>
    /// <returns>Awaitable task, complete once the caller holds the gate.</returns>
    public Task WaitAsync()
    {
        lock (_lock)
        {
            if (!_held)
            {
                _held = true;
                return Task.CompletedTask;
            }

            var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Enqueue(waiter);
            return waiter.Task;
        }
    }

    /// <summary>
    /// Frees the gate, handing it to the longest waiting caller when there is one.
    /// </summary>
    /// <exception cref="InvalidOperationException">The gate is not held.</exception>
    public void Release()
    {
        TaskCompletionSource? next;
        lock (_lock)
        {
            if (!_held)
            {
                throw new InvalidOperationException("The gate is not held.");
            }

            if (!_waiters.TryDequeue(out next))
            {
                _held = false;
                return;
            }
        }

        next.SetResult();
    }
}
