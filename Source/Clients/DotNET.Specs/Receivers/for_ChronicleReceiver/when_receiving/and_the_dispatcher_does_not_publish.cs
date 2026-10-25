// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class and_the_dispatcher_does_not_publish : given.a_receiver
{
    void Establish() => _reactors.Deliver(Arg.Any<ReactorId>(), Arg.Any<EventsToObserve>(), Arg.Any<CancellationToken>()).Returns((ReactorResult?)null);
    Task Because() => Receive();
    [Fact] void should_report_cancellation_not_success() => _outcome.Status.ShouldEqual(DeliveryStatus.Cancelled);
}
