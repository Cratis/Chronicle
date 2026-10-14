// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_an_unresolved_identifier : given.a_builder
{
    Exception _error;
    void Establish() => _builder.WithEventType("unknown").WithGenerationDelivery(EventGenerationDelivery.Pinned);
    void Because() => _error = Catch.Exception(() => _builder.Build());
    [Fact] void should_require_an_explicit_generation() => _error.ShouldBeOfExactType<EventStoreSubscriptionGenerationNotResolved>();
}
