// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_creating_for;

public class ReducerWithHandlerMethodForInterfaceEventTypeWithDerivatives
{
    public Task<ReadModel> Reduce(IMyEvent @event, ReadModel? initial, EventContext context) => Task.FromResult<ReadModel>(null!);
}
