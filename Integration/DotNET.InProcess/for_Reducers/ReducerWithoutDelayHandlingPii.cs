// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.InProcess.Integration.for_Reducers;

[DependencyInjection.IgnoreConvention]
public class ReducerWithoutDelayHandlingPii : IReducerFor<SomeReadModel>
{
    public int HandledEvents;
    public string LastSocialSecurityNumber = string.Empty;

    public Task<SomeReadModel?> OnPiiEvent(PiiEvent evt, SomeReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        LastSocialSecurityNumber = evt.SocialSecurityNumber;
        input ??= new SomeReadModel(0);
        return Task.FromResult<SomeReadModel?>(input with { Number = evt.Number });
    }

    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents < count)
        {
            await Task.Delay(50, cts.Token);
        }
    }
}
