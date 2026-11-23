// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_Reducers;

[DependencyInjection.IgnoreConvention]
public class ReducerThatCanFail(TaskCompletionSource tcs) : IReducerFor<SomeReadModel>
{
    public bool ShouldFail { get; set; }
    public TimeSpan HandleTime { get; set; } = TimeSpan.Zero;
    public int HandledEvents;

    public async Task<SomeReadModel?> OnSomeEvent(SomeEvent evt, SomeReadModel? input, EventContext ctx)
    {
        tcs.SetResult();

        // Task.Delay: time-behavior; configurable slow handler exercises failure and recovery while processing.
        await Task.Delay(HandleTime);
        if (ShouldFail)
        {
            ShouldFail = false;
            throw new Exception("Something went wrong");
        }

        Interlocked.Increment(ref HandledEvents);
        input ??= new SomeReadModel(0);
        return input with { Number = evt.Number };
    }

    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        await ReadinessWait.Until(() => Task.FromResult(HandledEvents), handled => handled == count, timeout);
    }
}
