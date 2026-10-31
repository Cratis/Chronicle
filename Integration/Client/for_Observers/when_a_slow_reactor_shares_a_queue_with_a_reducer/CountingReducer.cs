// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_Observers.when_a_slow_reactor_shares_a_queue_with_a_reducer;

/// <summary>
/// A pure, in-memory reducer over the same event as <see cref="GatedReactor"/>.
/// </summary>
[DependencyInjection.IgnoreConvention]
[Reducer(Id)]
public class CountingReducer : IReducerFor<ItemCount>
{
    /// <summary>
    /// The reducer id. Shares a queue with <see cref="GatedReactor.Id"/>.
    /// </summary>
    public const string Id = "fast-reducer-sharing-a-queue-1124";

    int _handled;

    public int Handled => Volatile.Read(ref _handled);

    public Task<ItemCount?> Registered(ItemRegistered evt, ItemCount? current, EventContext ctx)
    {
        Interlocked.Increment(ref _handled);
        return Task.FromResult<ItemCount?>(new((current?.Count ?? 0) + 1));
    }

    public async Task WaitTillHandled(int count, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            while (Handled < count)
            {
                await Task.Delay(20, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // The caller asserts on how far the reducer got.
        }
    }
}
