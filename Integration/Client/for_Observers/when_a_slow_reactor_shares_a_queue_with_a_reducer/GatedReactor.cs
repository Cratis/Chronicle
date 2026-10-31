// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Observers.when_a_slow_reactor_shares_a_queue_with_a_reducer;

/// <summary>
/// Stands in for a reactor calling a slow downstream service: every event waits until the gate opens.
/// </summary>
/// <remarks>
/// The id is chosen so that it lands on the same appended-events queue as <see cref="CountingReducer"/> for every
/// queue count from 1 to 8 (the kernel routes an observer by a stable hash of its id modulo the queue count).
/// </remarks>
[DependencyInjection.IgnoreConvention]
[Reactor(Id)]
public class GatedReactor : IReactor
{
    /// <summary>
    /// The reactor id. Shares a queue with <see cref="CountingReducer.Id"/>.
    /// </summary>
    public const string Id = "slow-reactor-sharing-a-queue";

    readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _started;
    int _handled;

    public int Started => Volatile.Read(ref _started);
    public int Handled => Volatile.Read(ref _handled);

    public void Open() => _gate.TrySetResult();

    public async Task Registered(ItemRegistered evt, EventContext ctx)
    {
        Interlocked.Increment(ref _started);
        await _gate.Task.WaitAsync(TimeSpan.FromSeconds(60));
        Interlocked.Increment(ref _handled);
    }
}
