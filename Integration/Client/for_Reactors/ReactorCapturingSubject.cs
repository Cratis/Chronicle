// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors;

[DependencyInjection.IgnoreConvention]
public class ReactorCapturingSubject(TaskCompletionSource<EventContext> tcs) : IReactor
{
    public Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        tcs.TrySetResult(ctx);
        return Task.CompletedTask;
    }
}
