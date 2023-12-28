// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation.for_ObserverInvoker.when_creating_for;

public class observer_with_handler_method_for_event_type : given.an_observer_invoker_for<ObserverWithHandlerMethodForEventType>
{
    [Fact] void should_have_the_event_type() => invoker.EventTypes.ShouldContainOnly(typeof(MyEvent).GetEventType());
}
