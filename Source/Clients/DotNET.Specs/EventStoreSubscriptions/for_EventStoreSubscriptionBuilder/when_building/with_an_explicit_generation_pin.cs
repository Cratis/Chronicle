// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_an_explicit_generation_pin : given.a_builder
{
    void Establish() => _builder.WithGenerationDelivery(EventGenerationDelivery.Pinned).WithEventType("external", 2);
    void Because() => _result = _builder.Build();
    [Fact] void should_preserve_the_explicit_generation() => _result.EventTypePins.ShouldContainOnly(new EventType("external", 2));
}
