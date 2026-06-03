// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_Reducers;

[DependencyInjection.IgnoreConvention]
public class SlowReducerThatSignalsCompletion(TaskCompletionSource completion) : IReducerFor<SomeReadModel>
{
    public int HandledEvents;
    public TimeSpan HandleTime { get; set; } = TimeSpan.Zero;
    public Task Completion => completion.Task;

    public async Task<SomeReadModel?> OnSomeEvent(SomeEvent evt, SomeReadModel? input, EventContext ctx)
    {
        await Task.Delay(HandleTime);
        Interlocked.Increment(ref HandledEvents);
        completion.TrySetResult();
        input ??= new SomeReadModel(0);
        return input with { Number = evt.Number };
    }
}
