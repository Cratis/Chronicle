// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.InProcess.Integration.for_Reducers;

[DependencyInjection.IgnoreConvention]
public class SynchronousReducerHandlingDeleteAsNull : IReducerFor<SomeReadModel>
{
    public SomeReadModel? OnSomeEvent(SomeEvent evt, SomeReadModel? input, EventContext ctx)
    {
        input ??= new SomeReadModel(evt.Number);
        return input with { Number = evt.Number };
    }

    public SomeReadModel? OnSomeDeleteEvent(SomeDeleteEvent evt, SomeReadModel? input, EventContext ctx) => null;
}


[DependencyInjection.IgnoreConvention]
public class AsynchronousReducerHandlingDeleteAsNull : IReducerFor<SomeReadModel>
{
    public Task<SomeReadModel?> OnSomeEvent(SomeEvent evt, SomeReadModel? input, EventContext ctx)
    {
        input ??= new SomeReadModel(evt.Number);
        return Task.FromResult(input with { Number = evt.Number });
    }

    public Task<SomeReadModel?> OnSomeDeleteEvent(SomeDeleteEvent evt, SomeReadModel? input, EventContext ctx) => Task.FromResult<SomeReadModel?>(null);
}


[DependencyInjection.IgnoreConvention]
public class AsynchronousReducerHandlingDeleteAsVoid : IReducerFor<SomeReadModel>
{
    public Task<SomeReadModel?> OnSomeEvent(SomeEvent evt, SomeReadModel? input, EventContext ctx)
    {
        input ??= new SomeReadModel(evt.Number);
        return Task.FromResult(input with { Number = evt.Number });
    }

    public Task OnSomeDeleteEvent(SomeDeleteEvent evt, SomeReadModel? input, EventContext ctx) => Task.CompletedTask;
}
