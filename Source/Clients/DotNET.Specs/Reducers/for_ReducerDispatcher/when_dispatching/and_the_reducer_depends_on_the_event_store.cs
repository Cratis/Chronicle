// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_the_reducer_depends_on_the_event_store : given.a_reducer_dispatcher_in_a_host
{
    async Task Because() => await _dispatcher.Dispatch(_handler, OperationWith(1), "namespace-x", Publish);

    [Fact] void should_give_the_reducer_the_delivered_namespace() => _log.ShouldContain("namespace:namespace-x");
    [Fact] void should_enter_the_delivery_scope_before_the_reducer_runs() => _log.ShouldEqual(["enter:namespace-x", "namespace:namespace-x", "leave"]);
    [Fact] void should_not_leave_a_delivery_context_behind() => Observation.ObserverDeliveryContext.Current.ShouldBeNull();
}
