// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation.for_ObserverInvoker.when_creating_for;

public class observer_with_no_handler_methods : given.an_observer_invoker_for<ObserverWithNoHandlerMethods>
{
    [Fact] void should_not_have_any_event_types() => invoker.EventTypes.ShouldBeEmpty();
}
