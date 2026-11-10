// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_keeping_its_subscription;

public class and_the_lost_subscription_of_a_client_that_takes_pages_is_restored : given.a_reducer_grain_with_a_stored_definition
{
    void Establish() => _observer.KnowsClient(_client.ConnectionId).Returns(false);

    Task Because() => _grain.KeepSubscription(_connectionDefinition with { PageDelivery = new PageDeliveryDefinition() });

    [Fact] Task should_restore_it_with_the_page_subscriber() => _observer.Received(1).RestoreClientSubscription<IReducerPageObserverSubscriber>(ObserverType.Reducer, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<Concepts.Clients.ConnectedClient>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<EventGenerationDelivery>());
    [Fact] void should_restore_it_only_with_the_page_subscriber() => _observer.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == nameof(IObserver.RestoreClientSubscription)).Select(_ => _.GetMethodInfo().GetGenericArguments()[0]).ShouldContainOnly(typeof(IReducerPageObserverSubscriber));
}
