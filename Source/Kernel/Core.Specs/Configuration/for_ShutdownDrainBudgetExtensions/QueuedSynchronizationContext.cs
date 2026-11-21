// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudgetExtensions;

/// <summary>
/// A synchronization context that holds back continuations until the spec runs them, so the spec decides whether a call
/// completes before or after the awaiting code resumes.
/// </summary>
public sealed class QueuedSynchronizationContext : SynchronizationContext
{
    readonly Queue<(SendOrPostCallback Callback, object? State)> _queued = new();

    /// <summary>
    /// Runs the given action with this context current, restoring the previous one afterwards.
    /// </summary>
    /// <param name="start">The action that starts the awaiting code.</param>
    public void Run(Action start)
    {
        var previous = Current;
        SetSynchronizationContext(this);
        try
        {
            start();
        }
        finally
        {
            SetSynchronizationContext(previous);
        }
    }

    /// <summary>
    /// Runs every continuation that was held back.
    /// </summary>
    public void ResumeAll()
    {
        while (_queued.TryDequeue(out var next))
        {
            next.Callback(next.State);
        }
    }

    /// <inheritdoc/>
    public override void Post(SendOrPostCallback d, object? state) => _queued.Enqueue((d, state));
}
