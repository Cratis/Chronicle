// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class without_explicit_type_selection : given.a_builder
{
    void Establish() => _builder.WithGenerationDelivery(EventGenerationDelivery.Pinned);
    void Because() => _result = _builder.Build();
    [Fact] void should_pin_the_client_declared_generations() => _result.EventTypePins.ShouldContainOnly(new EventType("customer", 3));
}
