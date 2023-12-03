// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootEventHandlers.when_creating_for;

public class aggregate_root_with_handler_method_for_event_type : given.aggregate_root_event_handlers_for<AggregateRootWithHandlerMethodForEventType>
{
    [Fact] void should_have_the_event_type() => handlers.EventTypes.ShouldContainOnly(typeof(MyEvent).GetEventType());
}
