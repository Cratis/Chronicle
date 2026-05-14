// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.InProcess.Integration.for_Reactors;

[DependencyInjection.IgnoreConvention]
public class ReactorWithoutDelayHandlingPii : IReactor
{
    public int HandledEvents;
    public string LastSocialSecurityNumber = string.Empty;

    public Task OnPiiEvent(PiiEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        LastSocialSecurityNumber = evt.SocialSecurityNumber;
        return Task.CompletedTask;
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
