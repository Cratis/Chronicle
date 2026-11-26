// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors;

[DependencyInjection.IgnoreConvention]
[FilterEventsByTag("important")]
public class ReactorFilteredByTag(TaskCompletionSource tcs) : IReactor
{
    public int HandledEvents;

    public async Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);

        // Task.Delay: time-behavior; simulate asynchronous handler latency before signaling completion.
        await Task.Delay(TimeSpan.FromMilliseconds(10));
        tcs.TrySetResult();
    }
}
