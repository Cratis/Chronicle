// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas.for_CompensationSchemaProcessor;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_compensation_schemas;

public class and_a_marked_target_has_a_registry_alias : given.registry_bindings
{
    JsonSchema _precise;
    JsonSchema _legacy;
    JsonSchema _declaredPrecise;
    JsonSchema _declaredLegacy;

    void Establish()
    {
        _first.HasFor(typeof(OriginalEvent)).Returns(true);
        _first.GetEventTypeFor(typeof(OriginalEvent)).Returns(new EventType("alias", 1));
        _declaredPrecise = _generator.Generate(typeof(CompensatingEvent));
        _declaredLegacy = _generator.GenerateLegacyEventType(typeof(CompensatingEvent));
    }

    void Because()
    {
        _precise = _generator.Generate(typeof(CompensatingEvent), _first);
        _legacy = _generator.GenerateLegacyEventType(typeof(CompensatingEvent), _first);
    }

    [Fact] void should_bind_precise_compensation_to_the_registry() => _precise.GetCompensationFor().ShouldEqual("alias");
    [Fact] void should_bind_legacy_compensation_to_the_registry() => _legacy.GetCompensationFor().ShouldEqual("alias");
    [Fact] void should_preserve_every_other_precise_schema_facet() => WithoutCompensation(_precise).ShouldEqual(WithoutCompensation(_declaredPrecise));
    [Fact] void should_preserve_every_other_legacy_schema_facet() => WithoutCompensation(_legacy).ShouldEqual(WithoutCompensation(_declaredLegacy));
    [Fact] void should_not_mutate_the_declared_precise_cache() => _declaredPrecise.GetCompensationFor().ShouldEqual(nameof(OriginalEvent));
    [Fact] void should_not_mutate_the_declared_legacy_cache() => _declaredLegacy.GetCompensationFor().ShouldEqual(nameof(OriginalEvent));

    static string WithoutCompensation(JsonSchema schema)
    {
        var node = JsonNode.Parse(schema.ToJson())!.AsObject();
        node.Remove(CompensationJsonSchemaExtensions.CompensationForKey);

        return node.ToJsonString();
    }
}
