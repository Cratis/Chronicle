// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Helper extensions providing wait methods for reducers.
/// </summary>
/// <remarks>
/// These extensions are very useful for integration testing purposes.
/// </remarks>
public static class ReducerWaitExtensions
{
    /// <summary>
    /// Wait for the reducer to reach a specific running state.
    /// </summary>
    /// <param name="reducer">Reducer to wait for.</param>
    /// <param name="runningState">The expected <see cref="ObserverRunningState"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitForState(this IReducerHandler reducer, ObserverRunningState runningState, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();

        var currentRunningState = ObserverRunningState.Unknown;
        using var cts = new CancellationTokenSource(timeout.Value);
        while (currentRunningState != runningState && !cts.IsCancellationRequested)
        {
            var state = await reducer.GetState();
            currentRunningState = state.RunningState;
            await Task.Delay(100, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the reducer is active, with an optional timeout.
    /// </summary>
    /// <param name="reducer">Reducer to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillActive(this IReducerHandler reducer, TimeSpan? timeout = default) =>
        reducer.WaitForState(ObserverRunningState.Active, timeout);

    /// <summary>
    /// Wait till the reactor has been subscribed, with an optional timeout.
    /// </summary>
    /// <param name="reducer">Reducer to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillSubscribed(this IReducerHandler reducer, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var state = await reducer.GetState();
            if (state.IsSubscribed)
            {
                break;
            }
            await Task.Delay(100, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the reducer reaches a specific event sequence number, with an optional timeout.
    /// </summary>
    /// <param name="reducer">Reducer to wait for.</param>
    /// <param name="eventSequenceNumber">The expected <see cref="EventSequenceNumber"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillReachesEventSequenceNumber(this IReducerHandler reducer, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var state = await reducer.GetState();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (state.LastHandledEventSequenceNumber != eventSequenceNumber && !cts.IsCancellationRequested)
        {
            state = await reducer.GetState();
            await Task.Delay(100, cts.Token);
        }
    }

    /// <summary>
    /// Wait for there to be failed partitions for a specific reducer, with an optional timeout.
    /// </summary>
    /// <param name="reducer">Reducer to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task<IEnumerable<FailedPartition>> WaitForThereToBeFailedPartitions(this IReducerHandler reducer, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var failedPartitions = await reducer.GetFailedPartitions();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (!failedPartitions.Any() && !cts.IsCancellationRequested)
        {
            failedPartitions = await reducer.GetFailedPartitions();
            await Task.Delay(100, cts.Token);
        }
        return failedPartitions;
    }

    /// <summary>
    /// Wait for the reducer to reach a specific running state.
    /// </summary>
    /// <param name="reducers">Reducer system to wait for the specific reducer for.</param>
    /// <param name="runningState">The expected <see cref="ObserverRunningState"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReducer">Type of reducer to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitForState<TReducer>(this IReducers reducers, ObserverRunningState runningState, TimeSpan? timeout = default)
        where TReducer : IReducer => reducers.GetHandlerFor<TReducer>().WaitForState(runningState, timeout);

    /// <summary>
    /// Wait till the reducer is active, with an optional timeout.
    /// </summary>
    /// <param name="reducers">Reducer system to wait for the specific reducer for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReducer">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillActive<TReducer>(this IReducers reducers, TimeSpan? timeout = default)
        where TReducer : IReducer => reducers.WaitForState<TReducer>(ObserverRunningState.Active, timeout);

    /// <summary>
    /// Wait till the reactor has been subscribed, with an optional timeout.
    /// </summary>
    /// <param name="reducers">Reducer system to wait for the specific reducer for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReducer">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillSubscribed<TReducer>(this IReducers reducers, TimeSpan? timeout = default)
        where TReducer : IReducer => reducers.GetHandlerFor<TReducer>().WaitTillSubscribed(timeout);

    /// <summary>
    /// Wait till the reducer reaches a specific event sequence number, with an optional timeout.
    /// </summary>
    /// <param name="reducers">Reducer system to wait for the specific reducer for.</param>
    /// <param name="eventSequenceNumber">The expected <see cref="EventSequenceNumber"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReducer">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillReachesEventSequenceNumber<TReducer>(this IReducers reducers, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
        where TReducer : IReducer => reducers.GetHandlerFor<TReducer>().WaitTillReachesEventSequenceNumber(eventSequenceNumber, timeout);

    /// <summary>
    /// Wait for there to be failed partitions for a specific reducer, with an optional timeout.
    /// </summary>
    /// <param name="reducers">Reducer system to wait for the specific reducer for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TReducer">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task<IEnumerable<FailedPartition>> WaitForThereToBeFailedPartitions<TReducer>(this IReducers reducers, TimeSpan? timeout = default)
        where TReducer : IReducer => reducers.GetHandlerFor<TReducer>().WaitForThereToBeFailedPartitions(timeout);
}
