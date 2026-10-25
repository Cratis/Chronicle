// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching;

public class and_the_delegate_fails : given.a_reactor_dispatcher
{
    int _calls;

    async Task Because() => await _dispatcher.Dispatch(
        _handler,
        (_, _) =>
        {
            _calls++;
            throw new Exception("Bridge unavailable");
        },
        EventsWith(1, 2),
        "ns",
        Publish,
        CancellationToken.None);

    [Fact] void should_publish_one_result() => _published.Count.ShouldEqual(1);
    [Fact] void should_fail() => _published[0].State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_not_advance_the_checkpoint() => _published[0].LastSuccessfulObservation.ShouldEqual(ulong.MaxValue);
    [Fact] void should_carry_the_message() => _published[0].ExceptionMessages.ShouldContain("Bridge unavailable");
    [Fact] void should_echo_the_partition() => _published[0].Partition.ShouldEqual("partition");
    [Fact] void should_echo_the_delivery_id() => _published[0].DeliveryId.ShouldEqual(42UL);
    [Fact] void should_stop_at_the_failing_event() => _calls.ShouldEqual(1);
}
