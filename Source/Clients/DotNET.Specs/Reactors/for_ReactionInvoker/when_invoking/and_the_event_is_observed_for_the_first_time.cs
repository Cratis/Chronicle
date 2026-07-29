// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

/// <summary>
/// Marking a replay handler must not change what happens on the live path.
/// </summary>
public class and_the_event_is_observed_for_the_first_time : given.a_reactor_with_a_replay_handler
{
    async Task Because() => await _invoker.Invoke(new MyEvent(), ContextObservedAs(EventObservationState.Initial));

    [Fact] void should_call_the_live_handler() => _reactor.LiveCalls.ShouldEqual(1);
    [Fact] void should_not_call_the_replay_handler() => _reactor.ReplayCalls.ShouldEqual(0);
}
