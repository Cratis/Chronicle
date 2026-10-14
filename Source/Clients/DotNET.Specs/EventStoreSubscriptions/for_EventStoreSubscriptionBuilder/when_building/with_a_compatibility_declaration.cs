// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_a_compatibility_declaration : given.a_builder
{
    void Establish() => _builder.WithEventType("unknown");
    void Because() => _result = _builder.Build();
    [Fact] void should_keep_compatibility_as_the_default() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
    [Fact] void should_allow_unknown_id_only_declarations() => _result.EventTypes.ShouldContainOnly(new EventTypeId("unknown"));
}
