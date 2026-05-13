// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Specifications.for_Reducers;

[DependencyInjection.IgnoreConvention]
public class SomeReducer(TaskCompletionSource tsc) : IReducerFor<SomeReadModel>
{
    public int HandledEvents;

    public async Task<SomeReadModel?> OnSomeEvent(SomeEvent evt, SomeReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        await Task.Delay(TimeSpan.FromMilliseconds(10));
        tsc.SetResult();

        input ??= new SomeReadModel(evt.Number);
        return input with { Number = evt.Number };
    }
}
