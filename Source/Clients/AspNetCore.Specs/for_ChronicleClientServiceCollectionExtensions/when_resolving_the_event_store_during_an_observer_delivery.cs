// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_resolving_the_event_store_during_an_observer_delivery : given.a_host_resolving_the_event_store
{
    EventStoreNamespaceName _result;

    void Because() => _result = ResolveNamespaceDuringDeliveryFor(new ChronicleAspNetCoreOptions().EventStore);

    [Fact] void should_resolve_the_delivered_namespace() => _result.ShouldEqual((EventStoreNamespaceName)"namespace-x");
    [Fact] void should_not_ask_the_namespace_resolver() => _namespaceResolver.DidNotReceive().Resolve();
}
