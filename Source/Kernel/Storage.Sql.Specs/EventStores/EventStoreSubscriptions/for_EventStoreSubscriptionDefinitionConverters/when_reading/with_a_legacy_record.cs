// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionConverters.when_reading;

public class with_a_legacy_record : Specification
{
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _result;
    void Because() => _result = JsonSerializer.Deserialize<EventStoreSubscriptionDefinition>("{\"Id\":\"subscription\",\"SourceEventStore\":\"source\",\"EventTypes\":[]}")!.ToKernel();
    [Fact] void should_default_to_compatibility() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
    [Fact] void should_keep_the_original_identifier() => _result.Identifier.Value.ShouldEqual("subscription");
}
