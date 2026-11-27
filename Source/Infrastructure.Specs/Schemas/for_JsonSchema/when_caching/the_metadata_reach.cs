// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_caching;

public class the_metadata_reach : Specification
{
    JsonSchemaProperty _property;
    JsonSchemaProperty _plain;
    JsonSchema _reference;
    JsonSchema _terminal;
    JsonSchema.MetadataReach _first;
    JsonSchema.MetadataReach _second;
    JsonSchema.MetadataReach _graph;
    JsonSchema.MetadataReach _sameGraph;
    bool _firstProtection;
    bool _secondProtection;
    bool _plainProtection;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""
            {
              "properties":{"value":{"$ref":"#/$defs/middle"},"plain":{"type":"string"}},
              "$defs":{
                "middle":{"$ref":"#/$defs/final"},
                "final":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}
              }
            }
            """);
        _property = schema.Properties["value"];
        _plain = schema.Properties["plain"];
        _reference = _property.Reference!;
        _terminal = _reference.Reference!;
    }

    void Because()
    {
        _first = _property.MetadataReferences;
        _second = _property.MetadataReferences;
        _graph = _property.MetadataGraph;
        _sameGraph = _property.MetadataGraph;
        _firstProtection = ConverterSchemas.HasProtectedValueMetadata(_property);
        _secondProtection = ConverterSchemas.HasProtectedValueMetadata(_property);
        _plainProtection = ConverterSchemas.HasProtectedValueMetadata(_plain);
    }

    [Fact] void should_reuse_the_same_reference_reach() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_reuse_the_same_full_graph() => ReferenceEquals(_graph, _sameGraph).ShouldBeTrue();
    [Fact] void should_keep_the_two_reach_modes_separate() => ReferenceEquals(_first, _graph).ShouldBeFalse();
    [Fact] void should_reuse_the_cached_reference_without_cloning() => ReferenceEquals(_first.Schemas[1], _reference).ShouldBeTrue();
    [Fact] void should_reuse_the_cached_terminal_without_cloning() => ReferenceEquals(_first.Schemas[2], _terminal).ShouldBeTrue();
    [Fact] void should_reuse_the_reference_in_the_full_graph() => ReferenceEquals(_graph.Schemas[1], _reference).ShouldBeTrue();
    [Fact] void should_reuse_the_terminal_in_the_full_graph() => ReferenceEquals(_graph.Schemas[2], _terminal).ShouldBeTrue();
    [Fact] void should_visit_each_reference_once() => _first.Schemas.Count.ShouldEqual(3);
    [Fact] void should_recognize_protection_on_the_first_call() => _firstProtection.ShouldBeTrue();
    [Fact] void should_recognize_protection_on_the_repeated_call() => _secondProtection.ShouldBeTrue();
    [Fact] void should_memoize_the_protected_answer() => _property.CachedHasProtectedValueMetadata.ShouldEqual(true);
    [Fact] void should_keep_plain_values_unprotected() => _plainProtection.ShouldBeFalse();
    [Fact] void should_memoize_the_unprotected_answer() => _plain.CachedHasProtectedValueMetadata.ShouldEqual(false);
}
