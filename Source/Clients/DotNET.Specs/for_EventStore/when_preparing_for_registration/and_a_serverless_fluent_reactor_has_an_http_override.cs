// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Serverless;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_a_serverless_fluent_reactor_has_an_http_override : given.an_explicit_store
{
    Exception _error;

    void Establish()
    {
        _options.UseServerless("orders-functions", new Uri("https://orders.example.com"));
        _options.Serverless.DefaultDelivery = ObserverDelivery.Stream;
        _options.Serverless.Observers["fluent-orders"] = new() { Delivery = ObserverDelivery.Http };
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _options.ExplicitArtifacts.RegisterReactor("fluent-orders", _ => _.On<PlainEvent>(_ => { }));
        _store = CreateStore();
    }

    async Task Because() => _error = await Catch.Exception(_store.DiscoverAll);

    [Fact] void should_fail_closed_during_startup_preparation() => _error.ShouldBeOfExactType<HttpDeliveryNotAvailable>();
    [Fact] void should_name_the_fluent_reactor() => _error.Message.ShouldContain("fluent-orders");
    [Fact] void should_publish_the_preparation_failure() => _store.Registration.Failure.ShouldEqual(_error);
    [Fact] void should_not_ensure_the_store() => _services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<EnsureEventStoreRequest>());
    [Fact] void should_not_activate_any_artifact() => _activator.ReceivedCalls().ShouldBeEmpty();
}
