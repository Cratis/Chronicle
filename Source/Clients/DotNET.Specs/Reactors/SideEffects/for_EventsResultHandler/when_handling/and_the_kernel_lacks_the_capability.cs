// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsResultHandler.when_handling;

public class and_the_kernel_lacks_the_capability : given.a_reactor_delivery_with_an_event_log
{
    protected override bool KernelSupportsIdempotentAppends => false;

    Result<ReactorSideEffectFailure> _first;
    Result<ReactorSideEffectFailure> _second;

    async Task Because()
    {
        var handler = new EventsResultHandler();
        _first = await handler.Handle(_reactorContext, _eventStore, new[] { new object(), new object() });
        _second = await handler.Handle(ContextFor(DeliveryAt(8UL)), _eventStore, new[] { new object() });
    }

    [Fact] void should_succeed_both_times() => (_first.IsSuccess && _second.IsSuccess).ShouldBeTrue();
    [Fact] void should_append_unkeyed_for_each_delivery() => UnkeyedAppendManyCalls.ShouldEqual(2);
    [Fact] void should_not_send_keyed_events() => KeyedBatches().ShouldBeEmpty();
    [Fact] void should_log_once_for_the_reactor_across_deliveries() => WarningsLogged.ShouldEqual(1);
}
