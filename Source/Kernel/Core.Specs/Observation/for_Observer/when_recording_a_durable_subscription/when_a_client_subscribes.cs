// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_a_durable_subscription;

public class when_a_client_subscribes : given.a_client_observer
{
    async Task Because() => await SubscribeClient();

    [Fact] void should_record_a_client_reactor() => Recorded.Kind.ShouldEqual(ObserverSubscriptionKind.ClientReactor);
    [Fact] void should_record_the_connection_it_delivers_to() => Recorded.Targets.Single().ConnectionId.ShouldEqual(_client.ConnectionId.Value);
    [Fact] void should_record_the_silo_that_terminates_the_connection() => Recorded.Targets.Single().Silo.ShouldEqual(SiloAddress.Zero.ToParsableString());
    [Fact] void should_not_record_page_delivery_for_a_reactor_that_takes_single_batches() => Recorded.DeliversPages.ShouldBeFalse();
}
