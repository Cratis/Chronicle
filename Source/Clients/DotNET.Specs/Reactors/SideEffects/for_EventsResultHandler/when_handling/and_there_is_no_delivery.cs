// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsResultHandler.when_handling;

public class and_there_is_no_delivery : given.a_reactor_delivery_with_an_event_log
{
    Result<ReactorSideEffectFailure> _result;

    async Task Because() => _result = await new EventsResultHandler().Handle(_reactorContext with { Delivery = null }, _eventStore, new[] { new object() });

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_unkeyed() => UnkeyedAppendManyCalls.ShouldEqual(1);
    [Fact] void should_not_log() => WarningsLogged.ShouldEqual(0);
}
