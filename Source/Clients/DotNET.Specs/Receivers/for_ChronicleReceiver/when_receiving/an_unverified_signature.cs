// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class an_unverified_signature : given.a_receiver
{
    async Task Because() => _outcome = await _receiver.Receive(new(_body, _target), CancellationToken.None);
    [Fact] void should_refuse_the_delivery() => new ProvisionalDeliveryStatusCodes().For(_outcome).ShouldEqual(503);
    [Fact] void should_not_resolve_any_store() => _client.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>());
    [Fact] void should_not_decode_the_body() => _decoder.DidNotReceive().Decode(Arg.Any<DeliveryRequest>(), Arg.Any<CancellationToken>());
}
