// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Checks before invocation and after successful completion, always joining the actual operation.
/// Faults propagate unchanged so invalidation cannot hide a genuine storage failure.
/// </summary>
/// <param name="lifetime">The activation that owns the continuation.</param>
internal sealed class ObserverContinuationCheckpoint(ObserverContinuationLifetime lifetime)
{
    /// <summary>Gets whether this checkpoint's activation is still current.</summary>
    internal bool IsCurrent => lifetime.IsCurrent;

    /// <summary>Rejects work on an invalidated activation.</summary>
    /// <exception cref="ObserverContinuationInvalidated">The activation was disposed.</exception>
    internal void ThrowIfInvalidated()
    {
        if (!IsCurrent) throw new ObserverContinuationInvalidated();
    }

    /// <summary>Invokes and joins an operation, checking before invocation and after success.</summary>
    /// <param name="operation">The operation factory, not an already-running task.</param>
    /// <returns>The joined operation.</returns>
    internal async Task Await(Func<Task> operation)
    {
        ThrowIfInvalidated();
        await operation();
        ThrowIfInvalidated();
    }

    /// <summary>Invokes and joins an operation, accepting its result only while current.</summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The operation factory, not an already-running task.</param>
    /// <returns>The result from a still-current continuation.</returns>
    internal async Task<T> Await<T>(Func<Task<T>> operation)
    {
        ThrowIfInvalidated();
        var result = await operation();
        ThrowIfInvalidated();
        return result;
    }
}
