// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookDefinitionConverters.when_reading;

public class with_a_legacy_record : Specification
{
    Concepts.Observation.Webhooks.WebhookDefinition _result;
    void Because() => _result = JsonSerializer.Deserialize<WebhookDefinition>("{\"Id\":\"webhook\",\"EventTypes\":{\"customer+3\":\"$eventSourceId\"}}")!.ToKernel();
    [Fact] void should_default_to_the_legacy_delivery_format() => _result.DeliveryFormat.ShouldEqual(WebhookDeliveryFormat.Legacy);
    [Fact] void should_default_to_compatibility() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
    [Fact] void should_preserve_the_declared_generation() => _result.EventTypes.Single().Generation.Value.ShouldEqual(3u);
}
