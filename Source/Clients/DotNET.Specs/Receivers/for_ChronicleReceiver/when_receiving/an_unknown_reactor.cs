// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class an_unknown_reactor : given.a_receiver
{
    void Establish() => _reactors.Deliver(Arg.Any<ReactorId>(), Arg.Any<EventsToObserve>(), Arg.Any<CancellationToken>()).Returns<ReactorResult?>(_ => throw new UnknownReactorId("missing"));
    Task Because() => Receive();
    [Fact] void should_report_unknown_observer() => _outcome.Status.ShouldEqual(DeliveryStatus.UnknownObserver);
}
