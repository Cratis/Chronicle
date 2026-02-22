// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_creating_for;

public class reactor_with_no_handler_methods : given.an_reactor_invoker_for<ReactorWithNoHandlerMethods>
{
    [Fact] void should_not_have_any_event_types() => _reactorEventTypes.ShouldBeEmpty();
}
