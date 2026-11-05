// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching;

public class and_a_replay_notification_is_delivered : given.a_reactor_dispatcher_in_a_host
{
    async Task Because() => await _dispatcher.Dispatch(_handler, null, new() { ReplayState = ReplayState.BeginReplay }, Delivered, Publish, CancellationToken.None);

    [Fact] void should_give_the_reactor_the_delivered_namespace() => _log.ShouldContain("namespace:namespace-x");
    [Fact] void should_enter_the_delivery_scope_before_constructing_the_reactor() => _log.IndexOf("enter:namespace-x").ShouldEqual(0);
    [Fact] void should_not_leave_a_delivery_context_behind() => Observation.ObserverDeliveryContext.Current.ShouldBeNull();
}
