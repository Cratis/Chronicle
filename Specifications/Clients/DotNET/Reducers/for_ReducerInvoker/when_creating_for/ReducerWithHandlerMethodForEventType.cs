// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers.for_ReducerInvoker.when_creating_for;

public class ReducerWithHandlerMethodForEventType
{
    public Task<ReadModel> Reduce(MyEvent @event, ReadModel? initial, EventContext context) => Task.FromResult<ReadModel>(null!);
}
