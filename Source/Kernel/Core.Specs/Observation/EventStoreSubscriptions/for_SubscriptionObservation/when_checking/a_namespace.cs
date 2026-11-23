// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_SubscriptionObservation.when_checking;

public class a_namespace : Specification
{
    [Fact] void should_observe_the_default_namespace() => SubscriptionObservation.Observes("Default").ShouldBeTrue();
    [Fact] void should_observe_a_tenant_namespace() => SubscriptionObservation.Observes("Tenant1").ShouldBeTrue();
    [Fact] void should_never_observe_a_namespace_with_a_comma() => SubscriptionObservation.Observes("raalabs,raalabs").ShouldBeFalse();
}
