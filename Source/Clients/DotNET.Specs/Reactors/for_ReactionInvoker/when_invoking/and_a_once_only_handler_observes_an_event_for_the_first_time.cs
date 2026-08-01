// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

/// <summary>
/// Once only suppresses the repeat, not the side effect itself - the first observation still has to run, or
/// marking a handler would stop it from ever doing its work.
/// </summary>
public class and_a_once_only_handler_observes_an_event_for_the_first_time : given.a_reactor_with_a_once_only_handler
{
    async Task Because() => await _invoker.Invoke(new MyEvent(), ContextObservedAs(EventObservationState.Initial));

    [Fact] void should_call_the_handler() => _reactor.Calls.ShouldEqual(1);
}
