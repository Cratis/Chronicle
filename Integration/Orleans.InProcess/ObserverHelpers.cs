// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;

namespace Cratis.Chronicle.Integration.Orleans.InProcess;

/// <summary>
/// Helper methods for working with observers for integration testing purposes.
/// </summary>
public static class ObserverHelpers
{
    /// <summary>
    /// Wait for the observer to reach a specific running state.
    /// </summary>
    /// <param name="observer">Observer to wait for.</param>
    /// <param name="runningState">The expected <see cref="ObserverRunningState"/> to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitForState(this IObserver observer, ObserverRunningState runningState, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var currentRunningState = ObserverRunningState.Unknown;
        using var cts = new CancellationTokenSource(timeout.Value);
        while (currentRunningState != runningState && !cts.IsCancellationRequested)
        {
            var state = await observer.GetState();
            currentRunningState = state.RunningState;
            await Task.Delay(20, cts.Token);
        }
    }

    /// <summary>
    /// Wait till the observer is active, with an optional timeout.
    /// </summary>
    /// <param name="observer">Observer to wait for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillActive(this IObserver observer, TimeSpan? timeout = default) =>
        await observer.WaitForState(ObserverRunningState.Active, timeout ?? TimeSpanFactory.DefaultTimeout());
}
