// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Helper extensions providing wait methods for projections.
/// </summary>
/// <remarks>
/// These extensions are very useful for integration testing purposes.
/// </remarks>
public static class ReactorWaitExtensions
{
    /// <summary>
    /// Wait for the reactor to reach a specific running state.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="runningState">The expected <see cref="ObserverRunningState"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitForState<TReactor>(this IReactors reactors, ObserverRunningState runningState, TimeSpan? timeout = default)
        where TReactor : IReactor
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();

        var currentRunningState = ObserverRunningState.Unknown;
        using var cts = new CancellationTokenSource(timeout.Value);
        while (currentRunningState != runningState && !cts.IsCancellationRequested)
        {
            var state = await reactors.GetStateFor<TReactor>();
            currentRunningState = state.RunningState;
            await Task.Delay(20, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the reactor is active, with an optional timeout.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillActive<TReactor>(this IReactors reactors, TimeSpan? timeout = default)
        where TReactor : IReactor =>
        await reactors.WaitForState<TReactor>(ObserverRunningState.Active, timeout);

    /// <summary>
    /// Wait till the reactor has been subscribed, with an optional timeout.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillSubscribed<TReactor>(this IReactors reactors, TimeSpan? timeout = default)
        where TReactor : IReactor
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var state = await reactors.GetStateFor<TReactor>();
            if (state.IsSubscribed)
            {
                break;
            }
            await Task.Delay(100, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the reactor reaches a specific event sequence number, with an optional timeout.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="eventSequenceNumber">The expected <see cref="EventSequenceNumber"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillReachesEventSequenceNumber<TReactor>(this IReactors reactors, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
        where TReactor : IReactor
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var state = await reactors.GetStateFor<TReactor>();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (state.LastHandledEventSequenceNumber != eventSequenceNumber && !cts.IsCancellationRequested)
        {
            state = await reactors.GetStateFor<TReactor>();
            await Task.Delay(20, cts.Token);
        }
    }

    /// <summary>
    /// Wait for there to be failed partitions for a specific reactor, with an optional timeout.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task<IEnumerable<FailedPartition>> WaitForThereToBeFailedPartitions<TReactor>(this IReactors reactors, TimeSpan? timeout = default)
        where TReactor : IReactor
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var failedPartitions = await reactors.GetFailedPartitionsFor<TReactor>();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (!failedPartitions.Any() && !cts.IsCancellationRequested)
        {
            failedPartitions = await reactors.GetFailedPartitionsFor<TReactor>();
            await Task.Delay(20, cts.Token);
        }
        return failedPartitions;
    }
}
