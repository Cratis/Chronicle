// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class an_unknown_event_store : given.a_receiver
{
    void Establish() => _delivery = _delivery with { Target = _target with { EventStore = "unknown" } };
    Task Because() => Receive();
    [Fact] void should_refuse_the_target() => _outcome.Status.ShouldEqual(DeliveryStatus.Malformed);
    [Fact] void should_not_resolve_or_create_any_store() => _client.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>());
    [Fact] void should_not_decode_the_body() => _decoder.DidNotReceive().Decode(Arg.Any<DeliveryRequest>(), Arg.Any<CancellationToken>());
}
