// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionConverters.when_round_tripping;

public class with_pinned_generations : Specification
{
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _definition;
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _result;
    void Establish() => _definition = new("subscription", "source", [new EventType("customer", 3), new EventType("order", 2)]) { GenerationDelivery = EventGenerationDelivery.Pinned };
    void Because() => _result = BsonSerializer.Deserialize<EventStoreSubscriptionDefinition>(_definition.ToMongoDB().ToBsonDocument()).ToKernel();
    [Fact] void should_preserve_the_mode() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_exact_generation_pins() => _result.EventTypes.ShouldContainOnly(_definition.EventTypes);
}
