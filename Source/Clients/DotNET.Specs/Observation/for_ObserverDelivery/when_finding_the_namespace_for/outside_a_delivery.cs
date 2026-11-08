// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery.when_finding_the_namespace_for;

public class outside_a_delivery : Specification
{
    EventStoreNamespaceName _result;

    void Because() => _result = ObserverDeliveryContext.NamespaceFor("store");

    [Fact] void should_return_nothing() => _result.ShouldBeNull();
}
