// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_a_generic_generation_pin : given.a_builder
{
    void Establish() => _builder.WithEventType<CustomerRegistered>().WithGenerationDelivery(EventGenerationDelivery.Pinned);
    void Because() => _result = _builder.Build();
    [Fact] void should_preserve_the_declared_generation() => _result.EventTypePins.ShouldContainOnly(new EventType("customer", 3));
    [Fact] void should_keep_the_id_only_surface() => _result.EventTypes.ShouldContainOnly(new EventTypeId("customer"));
    [Fact] void should_opt_in_per_subscription() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
}
