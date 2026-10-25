// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <summary>
    /// Runs a storage mutation of existing history with the mutation epoch odd while it runs and even after it.
    /// </summary>
    /// <remarks>
    /// The epoch is persisted before and after the mutation. A decision read that captured an earlier epoch is
    /// refused by <see cref="Concurrency.ConcurrencyValidator"/>, and a silo that dies mid-mutation leaves an odd epoch
    /// that is settled on the next activation, so no crash window can hide a change.
    /// </remarks>
    /// <typeparam name="T">The type returned by the mutation.</typeparam>
    /// <param name="mutation">The storage mutation.</param>
    /// <returns>The result of the mutation.</returns>
    async Task<T> Mutating<T>(Func<Task<T>> mutation)
    {
        State.MutationEpoch = State.MutationEpoch.Begin();
        await WriteStateAsync();
        try
        {
            return await mutation();
        }
        finally
        {
            State.MutationEpoch = State.MutationEpoch.End();
            await WriteStateAsync();
        }
    }

    /// <summary>
    /// Runs a storage mutation that returns nothing, advancing the mutation epoch around it.
    /// </summary>
    /// <param name="mutation">The storage mutation.</param>
    /// <returns>Awaitable task.</returns>
    async Task Mutating(Func<Task> mutation) =>
        await Mutating(async () =>
        {
            await mutation();
            return true;
        });

    /// <summary>
    /// Closes a mutation that was interrupted by the previous activation dying, so appends are validated against a settled epoch.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task SettleInterruptedMutation()
    {
        if (!State.MutationEpoch.IsMutating)
        {
            return;
        }

        State.MutationEpoch = State.MutationEpoch.Settled();
        await WriteStateAsync();
    }
}
