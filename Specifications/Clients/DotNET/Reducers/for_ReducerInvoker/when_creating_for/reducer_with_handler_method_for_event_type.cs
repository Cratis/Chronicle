// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Reducers.for_ReducerInvoker.when_creating_for;

public class reducer_with_handler_method_for_event_type : given.a_reducer_invoker_for<ReducerWithHandlerMethodForEventType>
{
    [Fact] void should_have_the_event_type() => invoker.EventTypes.ShouldContainOnly(typeof(MyEvent).GetEventType());
}
