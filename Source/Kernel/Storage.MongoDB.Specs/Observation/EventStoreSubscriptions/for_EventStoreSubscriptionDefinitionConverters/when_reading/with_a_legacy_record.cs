// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionConverters.when_reading;

public class with_a_legacy_record : Specification
{
    BsonDocument _document;
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _result;
    void Establish()
    {
        _document = new EventStoreSubscriptionDefinition { Id = "subscription", SourceEventStore = "source", EventTypes = [new EventType("customer", 1)] }.ToBsonDocument();
        var map = BsonClassMap.LookupClassMap(typeof(EventStoreSubscriptionDefinition));
        _document.Remove(map.GetMemberMap(nameof(EventStoreSubscriptionDefinition.GenerationDelivery)).ElementName);
    }
    void Because() => _result = BsonSerializer.Deserialize<EventStoreSubscriptionDefinition>(_document).ToKernel();
    [Fact] void should_default_to_compatibility() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
    [Fact] void should_keep_the_original_type_generation() => _result.EventTypes.Single().Generation.Value.ShouldEqual(1u);
}
