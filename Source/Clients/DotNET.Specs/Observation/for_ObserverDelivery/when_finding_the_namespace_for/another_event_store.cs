// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery.when_finding_the_namespace_for;

public class another_event_store : given.a_delivery_context
{
    EventStoreNamespaceName _result;

    void Because()
    {
        using var delivery = ObserverDeliveryScope.Enter(ProviderOf(), _context);
        _result = ObserverDeliveryContext.NamespaceFor("another-store");
    }

    [Fact] void should_return_nothing() => _result.ShouldBeNull();
}
