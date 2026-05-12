// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Specifications.for_Reducers;

[DependencyInjection.IgnoreConvention]
[EventStreamType("invoices")]
public class ReducerFilteredByEventStreamType(TaskCompletionSource tcs) : IReducerFor<SomeReadModel>
{
    public int HandledEvents;

    public async Task<SomeReadModel?> OnSomeEvent(SomeEvent evt, SomeReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        await Task.Delay(TimeSpan.FromMilliseconds(10));
        tcs.TrySetResult();

        input ??= new SomeReadModel(evt.Number);
        return input with { Number = evt.Number };
    }
}
