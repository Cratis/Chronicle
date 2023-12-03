// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootEventHandlers.when_creating_for;

public class aggregate_root_with_no_handler_methods : given.aggregate_root_event_handlers_for<AggregateRootWithNoHandlerMethods>
{
    [Fact] void should_not_have_any_event_types() => handlers.EventTypes.ShouldBeEmpty();
}
