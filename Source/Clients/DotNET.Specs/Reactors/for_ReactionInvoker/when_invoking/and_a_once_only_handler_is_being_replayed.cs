// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking;

/// <summary>
/// The attribute documents that a handler marked once only is excluded from replay. Placed on a method it is the
/// only place that exclusion can happen - the reactor stays replayable for the sake of its other handlers, so the
/// registration-time flag cannot express it.
/// </summary>
public class and_a_once_only_handler_is_being_replayed : given.a_reactor_with_a_once_only_handler
{
    async Task Because() => await _invoker.Invoke(new MyEvent(), ContextObservedAs(EventObservationState.Replay));

    [Fact] void should_not_call_the_handler() => _reactor.Calls.ShouldEqual(0);
}
