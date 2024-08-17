// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation;

namespace Cratis.Chronicle.Integration.Orleans.InProcess;

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

    public static async Task WaitTillActive(this IObserver observer, TimeSpan? timeout = default) => await observer.WaitForState(ObserverRunningState.Active, timeout);


    public static async Task WaitTillReachesEventSequenceNumber(this IObserver observer, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var state = await observer.GetState();
            if (state.LastHandledEventSequenceNumber.Value == eventSequenceNumber.Value)
            {
                break;
            }
            await Task.Delay(20, cts.Token);
        }
    }
}
