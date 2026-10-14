// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionConverters.when_round_tripping;

public class with_pinned_generations : Specification
{
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _definition;
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _result;
    void Establish() => _definition = new("subscription", "source", [new EventType("customer", 3), new EventType("order", 2)]) { GenerationDelivery = EventGenerationDelivery.Pinned };
    void Because() => _result = JsonSerializer.Deserialize<EventStoreSubscriptionDefinition>(JsonSerializer.Serialize(_definition.ToSql()))!.ToKernel();
    [Fact] void should_preserve_the_mode() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_the_exact_generations() => _result.EventTypes.ShouldContainOnly(_definition.EventTypes);
}
