// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_resolving_the_event_store_during_an_observer_delivery : given.a_host_resolving_the_event_store
{
    EventStoreNamespaceName _result;

    void Because()
    {
        using var delivery = ObserverDeliveryScope.Enter(_provider, new("store", "namespace-x", new("observer"), ObserverDeliveryKind.Reactor, typeof(object)));
        _result = ResolveNamespace();
    }

    [Fact] void should_resolve_the_delivered_namespace() => _result.ShouldEqual((EventStoreNamespaceName)"namespace-x");
    [Fact] void should_not_ask_the_namespace_resolver() => _namespaceResolver.DidNotReceive().Resolve();
}
