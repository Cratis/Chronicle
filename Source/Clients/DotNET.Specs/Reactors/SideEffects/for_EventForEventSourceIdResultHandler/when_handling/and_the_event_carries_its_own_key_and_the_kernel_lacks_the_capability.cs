// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventForEventSourceIdResultHandler.when_handling;

public class and_the_event_carries_its_own_key_and_the_kernel_lacks_the_capability : given.a_reactor_delivery_with_an_event_log
{
    protected override bool KernelSupportsIdempotentAppends => false;

    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await new EventForEventSourceIdResultHandler().Handle(
        _reactorContext,
        _eventStore,
        new EventForEventSourceId(EventSourceId.New(), new object()) { IdempotencyKey = _delivery.IdempotencyKeyFor("own") }));

    [Fact] void should_fail_the_partition() => _result.ShouldBeOfExactType<IdempotentAppendsNotSupported>();
    [Fact] void should_append_nothing() => _eventLog.ReceivedCalls().Count(call => call.GetMethodInfo().Name.StartsWith("Append")).ShouldEqual(0);
}
