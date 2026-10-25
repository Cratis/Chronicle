// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class a_malformed_body : given.a_receiver
{
    void Establish() => _decoder.Decode(Arg.Any<DeliveryRequest>(), Arg.Any<CancellationToken>()).Returns<Delivery>(_ => throw new InvalidDataException("safe reason"));
    Task Because() => Receive();
    [Fact] void should_report_malformed() => _outcome.Status.ShouldEqual(DeliveryStatus.Malformed);
    [Fact] void should_not_resolve_a_store() => _client.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>());
}
