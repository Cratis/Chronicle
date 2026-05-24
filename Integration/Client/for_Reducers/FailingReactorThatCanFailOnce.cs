// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reducers;

[DependencyInjection.IgnoreConvention]
public class FailingReactorThatCanFailOnce : IReactor
{
    public bool ShouldFail { get; set; }
    public int HandledEvents;

    public Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        if (ShouldFail)
        {
            ShouldFail = false;
            throw new Exception("Something went wrong");
        }

        Interlocked.Increment(ref HandledEvents);
        return Task.CompletedTask;
    }
}
