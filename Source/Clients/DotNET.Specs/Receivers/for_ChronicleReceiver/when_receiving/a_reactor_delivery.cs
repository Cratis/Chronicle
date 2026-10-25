// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Receivers.for_ChronicleReceiver.when_receiving;

public class a_reactor_delivery : given.a_receiver
{
    Task Because() => Receive();
    [Fact] void should_report_success() => _outcome.Status.ShouldEqual(DeliveryStatus.Succeeded);
    [Fact] void should_keep_the_last_observation() => _outcome.LastSuccessfulObservation.Value.ShouldEqual(12UL);
    [Fact] void should_request_the_explicit_tenant() => _client.Received(1).GetEventStore((EventStoreName)"store", (EventStoreNamespaceName)"tenant");
    [Fact] void should_forward_the_batch() => _reactors.Received(1).Deliver((ReactorId)"handler", Arg.Is<EventsToObserve>(batch => batch.Partition == "order-42" && batch.Events.Single() == _delivery.Events.Single()), CancellationToken.None);
}
