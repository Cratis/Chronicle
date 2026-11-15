// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class and_the_client_takes_pages : given.a_reducer_grain_with_a_stored_definition
{
    Task Because() => _grain.SetDefinitionAndSubscribe(_connectionDefinition with { PageDelivery = new PageDeliveryDefinition() });

    [Fact] Task should_subscribe_with_the_page_subscriber() => _observer.Received(1).Subscribe<IReducerPageObserverSubscriber>(ObserverType.Reducer, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<bool>(), Arg.Any<bool>());
    [Fact] void should_subscribe_only_with_the_page_subscriber() => SubscriberTypesOf(nameof(IObserver.Subscribe)).ShouldContainOnly(typeof(IReducerPageObserverSubscriber));

    IEnumerable<Type> SubscriberTypesOf(string method) => _observer.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == method).Select(_ => _.GetMethodInfo().GetGenericArguments()[0]);
}
