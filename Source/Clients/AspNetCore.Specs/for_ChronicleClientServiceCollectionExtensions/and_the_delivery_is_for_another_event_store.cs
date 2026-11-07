// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class and_the_delivery_is_for_another_event_store : given.a_host_resolving_the_event_store
{
    EventStoreNamespaceName _result;

    void Because() => _result = ResolveNamespaceDuringDeliveryFor("another-store");

    [Fact] void should_resolve_the_namespace_from_the_resolver() => _result.ShouldEqual((EventStoreNamespaceName)"namespace-y");
}
