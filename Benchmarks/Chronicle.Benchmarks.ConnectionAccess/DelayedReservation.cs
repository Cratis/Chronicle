// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Benchmarks.ConnectionAccess;

/// <summary>
/// Adds timer-based simulated network latency before each serialized reservation turn.
/// </summary>
/// <param name="inner">The real reservation state.</param>
/// <param name="latencyMilliseconds">The artificial round-trip latency.</param>
internal sealed class DelayedReservation(IConnectionReservation inner, int latencyMilliseconds) : IConnectionReservation
{
    readonly object _turn = new();

    /// <inheritdoc/>
    public Task<bool> IsHeldBy(ConnectionOwner owner)
    {
        if (latencyMilliseconds > 0) return DelayedCheck(owner);
        lock (_turn) return inner.IsHeldBy(owner);
    }

    /// <inheritdoc/>
    public Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task Release(ConnectionOwner owner, Guid token) => throw new NotSupportedException();

    async Task<bool> DelayedCheck(ConnectionOwner owner)
    {
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var timer = new Timer(_ => arrived.TrySetResult(), null, latencyMilliseconds, Timeout.Infinite);
        await arrived.Task;
        Task<bool> checkedOwner;
        lock (_turn) checkedOwner = inner.IsHeldBy(owner);
        return await checkedOwner;
    }
}
