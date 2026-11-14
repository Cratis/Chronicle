// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_IEventStoreSubscriptionBuilder;

public class when_restricting_source_namespaces_on_a_legacy_builder : given.a_legacy_builder
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => _builder.WithSourceNamespaces("Default"));
    [Fact] void should_fail_instead_of_observing_every_namespace() => _error.ShouldBeOfExactType<EventStoreSubscriptionSourceNamespacesNotSupported>();
}
