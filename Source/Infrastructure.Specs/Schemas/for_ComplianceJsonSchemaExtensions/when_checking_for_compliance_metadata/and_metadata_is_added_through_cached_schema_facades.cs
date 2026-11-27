// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_metadata_is_added_through_cached_schema_facades : Specification
{
    readonly List<(JsonSchema Root, JsonSchema Annotated)> _schemas = [];
    readonly List<bool> _discovered = [];
    readonly List<bool> _protected = [];
    readonly List<bool> _reused = [];

    void Establish()
    {
        var property = JsonSchema.FromJson("""{"properties":{"value":{"type":"string"}}}""");
        Add(property, property.Properties["value"]);
        var nested = JsonSchema.FromJson("""{"properties":{"nested":{"properties":{"value":{"type":"string"}}}}}""");
        Add(nested, nested.Properties["nested"].Properties["value"]);
        var items = JsonSchema.FromJson("""{"type":"array","items":{"type":"array","items":{"type":"string"}}}""");
        Add(items, items.Item!.Item!);
        var allOf = JsonSchema.FromJson("""{"allOf":[{"properties":{"value":{"type":"string"}}}]}""");
        Add(allOf, allOf.AllOf[0].Properties["value"]);
        var anyOf = JsonSchema.FromJson("""{"anyOf":[{"type":"null"},{"type":"string"}]}""");
        Add(anyOf, anyOf.AnyOf[1]);
        var contextual = JsonSchema.FromJson("""{"oneOf":[{"type":"string"}],"$dynamicRef":"#/$defs/plain","$defs":{"plain":{"type":"string"}}}""");
        Add(contextual, contextual.OneOf[0]);
        var constructed = new JsonSchema();
        constructed.Properties["value"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        Add(constructed, constructed.Properties["value"]);
    }

    void Because()
    {
        foreach (var (root, annotated) in _schemas)
        {
            _discovered.Add(root.HasComplianceMetadata());
            _protected.Add(ConverterSchemas.HasProtectedValueMetadata(annotated));
            _reused.Add(root.MetadataGraph.Schemas.Exists(candidate => ReferenceEquals(candidate, annotated)));
        }
    }

    [Fact] void should_cover_properties_nested_items_compositions_and_constructed_schemas() => _schemas.Count.ShouldEqual(7);
    [Fact] void should_find_each_annotation() => _discovered.TrueForAll(value => value).ShouldBeTrue();
    [Fact] void should_preserve_conversion_protection() => _protected.TrueForAll(value => value).ShouldBeTrue();
    [Fact] void should_read_the_existing_facades_without_cloning() => _reused.TrueForAll(value => value).ShouldBeTrue();

    void Add(JsonSchema root, JsonSchema annotated)
    {
        annotated.ExtensionData = new Dictionary<string, object?>
        {
            [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
        };
        _schemas.Add((root, annotated));
    }
}
