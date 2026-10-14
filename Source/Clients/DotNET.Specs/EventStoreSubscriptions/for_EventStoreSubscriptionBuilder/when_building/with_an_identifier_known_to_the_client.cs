// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_an_identifier_known_to_the_client : given.a_builder
{
    void Establish() => _builder.WithEventType("customer").WithGenerationDelivery(EventGenerationDelivery.Pinned);
    void Because() => _result = _builder.Build();
    [Fact] void should_resolve_the_declared_generation() => _result.EventTypePins.ShouldContainOnly(new EventType("customer", 3));
}
