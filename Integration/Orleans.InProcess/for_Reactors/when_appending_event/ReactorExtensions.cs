// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_appending_event;

public static class ReactorExtensions
{
    public static async Task WaitForState<TReactor>(this IReactors reactors, ObserverRunningState runningState, TimeSpan? timeout = default)
        where TReactor : IReactor
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();

        var currentRunningState = ObserverRunningState.Unknown;
        using var cts = new CancellationTokenSource(timeout.Value);
        while (currentRunningState != runningState && !cts.IsCancellationRequested)
        {
            var state = await reactors.GetState<TReactor>();
            currentRunningState = state.RunningState;
            await Task.Delay(20, cts.Token);
        }
    }

    public static async Task WaitTillActive<TReactor>(this IReactors reactors, TimeSpan? timeout = default)
        where TReactor : IReactor =>
        await reactors.WaitForState<TReactor>(ObserverRunningState.Active, timeout);

    public static async Task WaitTillReachesEventSequenceNumber<TReactor>(this IReactors reactors, EventSequenceNumber eventSequenceNumber, TimeSpan? timeout = default)
        where TReactor : IReactor
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var state = await reactors.GetState<TReactor>();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (state.NextEventSequenceNumber != eventSequenceNumber && !cts.IsCancellationRequested)
        {
            state = await reactors.GetState<TReactor>();
            await Task.Delay(20, cts.Token);
        }
    }
}
