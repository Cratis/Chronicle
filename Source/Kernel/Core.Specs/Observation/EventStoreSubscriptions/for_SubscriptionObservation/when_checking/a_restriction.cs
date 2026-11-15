// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_SubscriptionObservation.when_checking;

public class a_restriction : Specification
{
    EventStoreSubscriptionDefinition _unrestricted;
    EventStoreSubscriptionDefinition _restricted;

    void Establish()
    {
        _unrestricted = new("s", "Source", [EventType.Unknown]);
        _restricted = _unrestricted with { SourceNamespaces = ["Default", "Tenant1"] };
    }

    [Fact] void should_observe_every_namespace_without_a_restriction() => SubscriptionObservation.Observes(_unrestricted, "Anything").ShouldBeTrue();
    [Fact] void should_observe_a_listed_namespace_ignoring_case() => SubscriptionObservation.Observes(_restricted, "default").ShouldBeTrue();
    [Fact] void should_not_observe_an_unlisted_namespace() => SubscriptionObservation.Observes(_restricted, "Tenant2").ShouldBeFalse();
    [Fact] void should_never_observe_a_namespace_with_a_comma_without_a_restriction() => SubscriptionObservation.Observes(_unrestricted, "a,a").ShouldBeFalse();
    [Fact] void should_never_observe_a_namespace_with_a_comma_even_if_listed() => SubscriptionObservation.Observes(_unrestricted with { SourceNamespaces = ["a,a"] }, "a,a").ShouldBeFalse();
}
