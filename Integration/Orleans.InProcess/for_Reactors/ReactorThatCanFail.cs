// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors;

[DependencyInjection.IgnoreConvention]
public class ReactorThatCanFail(TaskCompletionSource tcs) : IReactor
{
    public bool ShouldFail { get; set; }

    public Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        tcs.SetResult();
        if (ShouldFail)
        {
            ShouldFail = false;
            throw new Exception("Something went wrong");
        }

        return Task.CompletedTask;
    }
}
