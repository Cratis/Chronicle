// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class an_already_cancelled_request : given.a_receiver
{
    Task Because() => Receive(new CancellationToken(true));
    [Fact] void should_report_cancellation() => _outcome.Status.ShouldEqual(DeliveryStatus.Cancelled);
    [Fact] void should_not_resolve_a_store() => _client.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>());
}
