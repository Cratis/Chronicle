// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Serverless;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_a_serverless_explicit_reducer_has_an_http_override : given.an_explicit_store
{
    Exception _error;

    void Establish()
    {
        _options.UseServerless("orders-functions", new Uri("https://orders.example.com"));
        _options.Serverless.DefaultDelivery = ObserverDelivery.Stream;
        _options.Serverless.Observers["explicit-orders"] = new() { Delivery = ObserverDelivery.Http };
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>().RegisterReducer<PlainReducer>("explicit-orders");
        _store = CreateStore();
    }

    async Task Because() => _error = await Catch.Exception(_store.DiscoverAll);

    public record Order(string Name);

    public class PlainReducer
    {
        public Order Reduce(PlainEvent @event, Order current) => new(@event.Name);
    }

    [Fact] void should_fail_closed_during_startup_preparation() => _error.ShouldBeOfExactType<HttpDeliveryNotAvailable>();
    [Fact] void should_name_the_explicit_reducer() => _error.Message.ShouldContain("explicit-orders");
    [Fact] void should_publish_the_preparation_failure() => _store.Registration.Failure.ShouldEqual(_error);
    [Fact] void should_not_ensure_the_store() => _services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
    [Fact] void should_not_activate_any_artifact() => _activator.ReceivedCalls().ShouldBeEmpty();
}
