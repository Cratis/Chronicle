// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Specifications.for_Reducers;

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
