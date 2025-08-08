// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Helper extensions providing wait methods for projections.
/// </summary>
/// <remarks>
/// These extensions are very useful for integration testing purposes.
/// </remarks>
public static class ProjectionWaitExtensions
{
    const int DefaultDelay = 50;

    /// <summary>
    /// Wait for the projection to reach a specific running state.
    /// </summary>
    /// <param name="projection">Projection to wait for.</param>
    /// <param name="runningState">The expected <see cref="ObserverRunningState"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitForState(this IProjectionHandler projection, ObserverRunningState runningState, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();

        var currentRunningState = ObserverRunningState.Unknown;
        using var cts = new CancellationTokenSource(timeout.Value);
        while (currentRunningState != runningState && !cts.IsCancellationRequested)
        {
            var state = await projection.GetState();
            currentRunningState = state.RunningState;
            await Task.Delay(DefaultDelay, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the projection is active, with an optional timeout.
    /// </summary>
    /// <param name="projection">Projection to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillActive(this IProjectionHandler projection, TimeSpan? timeout = default) =>
        projection.WaitForState(ObserverRunningState.Active, timeout);

    /// <summary>
    /// Wait till the reactor has been subscribed, with an optional timeout.
    /// </summary>
    /// <param name="projection">Projection to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillSubscribed(this IProjectionHandler projection, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var state = await projection.GetState();
            if (state.IsSubscribed)
            {
                break;
            }
            await Task.Delay(DefaultDelay, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the projection reaches a specific event sequence number, with an optional timeout.
    /// </summary>
    /// <param name="projection">Projection to wait for.</param>
    /// <param name="eventSequenceNumber">The expected <see cref="EventSequenceNumber"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillReachesEventSequenceNumber(this IProjectionHandler projection, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var state = await projection.GetState();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (state.LastHandledEventSequenceNumber != eventSequenceNumber && !cts.IsCancellationRequested)
        {
            state = await projection.GetState();
            await Task.Delay(DefaultDelay, cts.Token);
        }
    }

    /// <summary>
    /// Wait for there to be failed partitions for a specific projection, with an optional timeout.
    /// </summary>
    /// <param name="projection">Projection to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task<IEnumerable<FailedPartition>> WaitForThereToBeFailedPartitions(this IProjectionHandler projection, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var failedPartitions = await projection.GetFailedPartitions();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (!failedPartitions.Any() && !cts.IsCancellationRequested)
        {
            failedPartitions = await projection.GetFailedPartitions();
            await Task.Delay(DefaultDelay, cts.Token);
        }
        return failedPartitions;
    }

    /// <summary>
    /// Wait for the projection to reach a specific running state.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="runningState">The expected <see cref="ObserverRunningState"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of projection to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitForState<TProjection>(this IProjections projections, ObserverRunningState runningState, TimeSpan? timeout = default)
        where TProjection : IProjection => projections.GetHandlerFor<TProjection>().WaitForState(runningState, timeout);

    /// <summary>
    /// Wait till the projection is active, with an optional timeout.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillActive<TProjection>(this IProjections projections, TimeSpan? timeout = default)
        where TProjection : IProjection => projections.WaitForState<TProjection>(ObserverRunningState.Active, timeout);

    /// <summary>
    /// Wait till the reactor has been subscribed, with an optional timeout.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillSubscribed<TProjection>(this IProjections projections, TimeSpan? timeout = default)
        where TProjection : IProjection => projections.GetHandlerFor<TProjection>().WaitTillSubscribed(timeout);

    /// <summary>
    /// Wait till the projection reaches a specific event sequence number, with an optional timeout.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="eventSequenceNumber">The expected <see cref="EventSequenceNumber"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task WaitTillReachesEventSequenceNumber<TProjection>(this IProjections projections, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
        where TProjection : IProjection => projections.GetHandlerFor<TProjection>().WaitTillReachesEventSequenceNumber(eventSequenceNumber, timeout);

    /// <summary>
    /// Wait for there to be failed partitions for a specific projection, with an optional timeout.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static Task<IEnumerable<FailedPartition>> WaitForThereToBeFailedPartitions<TProjection>(this IProjections projections, TimeSpan? timeout = default)
        where TProjection : IProjection => projections.GetHandlerFor<TProjection>().WaitForThereToBeFailedPartitions(timeout);
}
