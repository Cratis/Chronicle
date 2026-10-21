// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_conflicting_generations : given.a_builder
{
    Exception _error;
    void Establish() => _builder.WithEventType<CustomerRegistered>().WithEventType("customer", 2).WithGenerationDelivery(EventGenerationDelivery.Pinned);
    void Because() => _error = Catch.Exception(() => _builder.Build());
    [Fact] void should_reject_ambiguous_pins() => _error.ShouldBeOfExactType<MultipleGenerationsOfEventTypeInObserver>();
}
