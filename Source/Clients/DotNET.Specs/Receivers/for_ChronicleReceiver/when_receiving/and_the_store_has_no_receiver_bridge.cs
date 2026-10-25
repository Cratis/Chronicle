// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class and_the_store_has_no_receiver_bridge : given.a_receiver
{
    void Establish() => _store.Reactors.Returns(Substitute.For<IReactors>());
    Task Because() => Receive();
    [Fact] void should_report_unknown_observer() => _outcome.Status.ShouldEqual(DeliveryStatus.UnknownObserver);
}
