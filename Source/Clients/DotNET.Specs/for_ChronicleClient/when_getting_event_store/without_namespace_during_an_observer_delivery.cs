// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.for_ChronicleClient.when_getting_event_store;

public class without_namespace_during_an_observer_delivery : Specification
{
    IEventStoreNamespaceResolver _namespaceResolver;
    ChronicleClient _client;
    int _resolutionsAfterTheDeliveredStore;
    int _resolutionsAfterAnExplicitNamespace;
    int _resolutionsAfterAnotherStore;

    void Establish()
    {
        _namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        _namespaceResolver.Resolve().Returns((EventStoreNamespaceName)"namespace-y");
        _client = new ChronicleClient(new ChronicleOptions { AutoDiscoverAndRegister = false }, namespaceResolver: _namespaceResolver);
    }

    void Because()
    {
        using var delivery = ObserverDeliveryScope.Enter(new DefaultServiceProvider(), new("TestStore", "namespace-x", new("observer"), ObserverDeliveryKind.Reactor, typeof(object)));
        _ = _client.GetEventStore("TestStore");
        _resolutionsAfterTheDeliveredStore = _namespaceResolver.ReceivedCalls().Count();
        _ = _client.GetEventStore("TestStore", "namespace-z");
        _resolutionsAfterAnExplicitNamespace = _namespaceResolver.ReceivedCalls().Count();
        _ = _client.GetEventStore("OtherStore");
        _resolutionsAfterAnotherStore = _namespaceResolver.ReceivedCalls().Count();
    }

    [Fact] void should_not_ask_the_resolver_for_the_delivered_store() => _resolutionsAfterTheDeliveredStore.ShouldEqual(0);
    [Fact] void should_not_ask_the_resolver_for_an_explicit_namespace() => _resolutionsAfterAnExplicitNamespace.ShouldEqual(0);
    [Fact] void should_ask_the_resolver_for_another_store() => _resolutionsAfterAnotherStore.ShouldEqual(1);
}
