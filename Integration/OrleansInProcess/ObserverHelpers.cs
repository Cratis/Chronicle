// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;

namespace Cratis.Chronicle.Integration.OrleansInProcess;

public static class ObserverHelpers
{
    public static async Task WaitForState(this IObserver observer, ObserverRunningState runningState, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        var currentRunningState = ObserverRunningState.New;
        using var cts = new CancellationTokenSource(timeout.Value);
        while (currentRunningState != runningState && !cts.IsCancellationRequested)
        {
            var state = await observer.GetState();
            currentRunningState = state.RunningState;
            await Task.Delay(20, cts.Token);
        }
    }
}