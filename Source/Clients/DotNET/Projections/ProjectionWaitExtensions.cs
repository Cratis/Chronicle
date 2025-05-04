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
    /// <summary>
    /// Wait for the projection to reach a specific running state.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="runningState">The expected <see cref="ObserverRunningState"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of projection to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitForState<TProjection>(this IProjections projections, ObserverRunningState runningState, TimeSpan? timeout = default)
        where TProjection : IProjection
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();

        var currentRunningState = ObserverRunningState.Unknown;
        using var cts = new CancellationTokenSource(timeout.Value);
        while (currentRunningState != runningState && !cts.IsCancellationRequested)
        {
            var state = await projections.GetStateFor<TProjection>();
            currentRunningState = state.RunningState;
            await Task.Delay(20, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the projection is active, with an optional timeout.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillActive<TProjection>(this IProjections projections, TimeSpan? timeout = default)
        where TProjection : IProjection =>
        await projections.WaitForState<TProjection>(ObserverRunningState.Active, timeout);

    /// <summary>
    /// Wait till the reactor has been subscribed, with an optional timeout.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillSubscribed<TProjection>(this IProjections projections, TimeSpan? timeout = default)
        where TProjection : IProjection
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var state = await projections.GetStateFor<TProjection>();
            if (state.IsSubscribed)
            {
                break;
            }
            await Task.Delay(100, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the projection reaches a specific event sequence number, with an optional timeout.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="eventSequenceNumber">The expected <see cref="EventSequenceNumber"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillReachesEventSequenceNumber<TProjection>(this IProjections projections, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
        where TProjection : IProjection
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var state = await projections.GetStateFor<TProjection>();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (state.LastHandledEventSequenceNumber != eventSequenceNumber && !cts.IsCancellationRequested)
        {
            state = await projections.GetStateFor<TProjection>();
            await Task.Delay(20, cts.Token);
        }
    }

    /// <summary>
    /// Wait for there to be failed partitions for a specific projection, with an optional timeout.
    /// </summary>
    /// <param name="projections">Projection system to wait for the specific projection for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <typeparam name="TProjection">Type of reactor to wait for.</typeparam>
    /// <returns>Awaitable task.</returns>
    public static async Task<IEnumerable<FailedPartition>> WaitForThereToBeFailedPartitions<TProjection>(this IProjections projections, TimeSpan? timeout = default)
        where TProjection : IProjection
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var failedPartitions = await projections.GetFailedPartitionsFor<TProjection>();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (!failedPartitions.Any() && !cts.IsCancellationRequested)
        {
            failedPartitions = await projections.GetFailedPartitionsFor<TProjection>();
            await Task.Delay(20, cts.Token);
        }
        return failedPartitions;
    }
}
