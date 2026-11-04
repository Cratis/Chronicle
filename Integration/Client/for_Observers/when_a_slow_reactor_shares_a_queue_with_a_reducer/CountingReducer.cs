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

    readonly TaskCompletionSource _reachedExpected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _expected = int.MaxValue;
    int _handled;

    public int Handled => Volatile.Read(ref _handled);

    public Task<ItemCount?> Registered(ItemRegistered evt, ItemCount? current, EventContext ctx)
    {
        if (Interlocked.Increment(ref _handled) >= Volatile.Read(ref _expected))
        {
            _reachedExpected.TrySetResult();
        }

        return Task.FromResult<ItemCount?>(new((current?.Count ?? 0) + 1));
    }

    public async Task WaitTillHandled(int count, TimeSpan timeout)
    {
        Volatile.Write(ref _expected, count);
        if (Handled >= count)
        {
            return;
        }

        try
        {
            await _reachedExpected.Task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            // The caller asserts on how far the reducer got.
        }
    }
}
