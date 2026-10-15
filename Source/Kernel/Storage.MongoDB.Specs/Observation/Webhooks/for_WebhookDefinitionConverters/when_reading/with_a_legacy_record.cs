// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookDefinitionConverters.when_reading;

public class with_a_legacy_record : Specification
{
    BsonDocument _document;
    Concepts.Observation.Webhooks.WebhookDefinition _result;

    void Establish()
    {
        _document = new WebhookDefinition { EventTypes = new Dictionary<string, string> { ["customer+3"] = "$eventSourceId" } }.ToBsonDocument();
        var map = BsonClassMap.LookupClassMap(typeof(WebhookDefinition));
        _document.Remove(map.GetMemberMap(nameof(WebhookDefinition.GenerationDelivery)).ElementName);
    }

    void Because() => _result = BsonSerializer.Deserialize<WebhookDefinition>(_document).ToKernel();
    [Fact] void should_default_to_compatibility() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
    [Fact] void should_preserve_the_declared_generation() => _result.EventTypes.Single().Generation.Value.ShouldEqual(3u);
}
