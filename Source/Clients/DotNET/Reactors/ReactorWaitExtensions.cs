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
    const int DefaultDelay = 50;

    /// <summary>
    /// Wait for the reactor to reach a specific running state.
    /// </summary>
    /// <param name="reactor">Reactor to wait for.</param>
    /// <param name="runningState">The expected <see cref="ObserverRunningState"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitForState(this IReactorHandler reactor, ObserverRunningState runningState, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();

        var currentRunningState = ObserverRunningState.Unknown;
        using var cts = new CancellationTokenSource(timeout.Value);
        while (currentRunningState != runningState && !cts.IsCancellationRequested)
        {
            var state = await reactor.GetState();
            currentRunningState = state.RunningState;
            await Task.Delay(DefaultDelay, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the reactor is active, with an optional timeout.
    /// </summary>
    /// <param name="reactor">Reactor to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillActive(this IReactorHandler reactor, TimeSpan? timeout = default) =>
         reactor.WaitForState(ObserverRunningState.Active, timeout);

    /// <summary>
    /// Wait till the reactor has been subscribed, with an optional timeout.
    /// </summary>
    /// <param name="reactor">Reactor to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillSubscribed(this IReactorHandler reactor, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var state = await reactor.GetState();
            if (state.IsSubscribed)
            {
                break;
            }
            await Task.Delay(DefaultDelay, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the reactor reaches a specific event sequence number, with an optional timeout.
    /// </summary>
    /// <param name="reactor">Reactor to wait for.</param>
    /// <param name="eventSequenceNumber">The expected <see cref="EventSequenceNumber"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillReachesEventSequenceNumber(this IReactorHandler reactor, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var state = await reactor.GetState();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (state.LastHandledEventSequenceNumber != eventSequenceNumber && !cts.IsCancellationRequested)
        {
            state = await reactor.GetState();
            await Task.Delay(DefaultDelay, cts.Token);
        }
    }

    /// <summary>
    /// Wait for there to be failed partitions for a specific reactor, with an optional timeout.
    /// </summary>
    /// <param name="reactor">Reactor to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task<IEnumerable<FailedPartition>> WaitForThereToBeFailedPartitions(this IReactorHandler reactor, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var failedPartitions = await reactor.GetFailedPartitions();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (!failedPartitions.Any() && !cts.IsCancellationRequested)
        {
            failedPartitions = await reactor.GetFailedPartitions();
            await Task.Delay(DefaultDelay, cts.Token);
        }
        return failedPartitions;
    }

    /// <summary>
    /// Wait for the reactor to reach a specific running state.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="runningState">The expected <see cref="ObserverRunningState"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitForState<TReactor>(this IReactors reactors, ObserverRunningState runningState, TimeSpan? timeout = default)
        where TReactor : IReactor => reactors.GetHandlerFor<TReactor>().WaitForState(runningState, timeout);

    /// <summary>
    /// Wait till the reactor is active, with an optional timeout.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillActive<TReactor>(this IReactors reactors, TimeSpan? timeout = default)
        where TReactor : IReactor => reactors.WaitForState<TReactor>(ObserverRunningState.Active, timeout);

    /// <summary>
    /// Wait till the reactor has been subscribed, with an optional timeout.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillSubscribed<TReactor>(this IReactors reactors, TimeSpan? timeout = default)
        where TReactor : IReactor => reactors.GetHandlerFor<TReactor>().WaitTillSubscribed(timeout);

    /// <summary>
    /// Wait till the reactor reaches a specific event sequence number, with an optional timeout.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="eventSequenceNumber">The expected <see cref="EventSequenceNumber"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillReachesEventSequenceNumber<TReactor>(this IReactors reactors, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
        where TReactor : IReactor => reactors.GetHandlerFor<TReactor>().WaitTillReachesEventSequenceNumber(eventSequenceNumber, timeout);

    /// <summary>
    /// Wait for there to be failed partitions for a specific reactor, with an optional timeout.
    /// </summary>
    /// <param name="reactors">Reactor system to wait for the specific reactor for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReactor">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task<IEnumerable<FailedPartition>> WaitForThereToBeFailedPartitions<TReactor>(this IReactors reactors, TimeSpan? timeout = default)
        where TReactor : IReactor => reactors.GetHandlerFor<TReactor>().WaitForThereToBeFailedPartitions(timeout);
}
