// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_GenerationProtectionSchemas.when_resolving;

public class and_array_items_are_plain_strings : Specification
{
    JsonSchema _own;
    JsonSchema _higher;
    JsonSchema _effective;

    void Establish()
    {
        _own = JsonSchema.FromJson("""{"type":"object","properties":{"tokens":{"type":"array","items":{"type":"string"}}}}""");
        _higher = JsonSchema.FromJson("""{"type":"object","properties":{"tokens":{"type":"array","items":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}}""");
    }

    void Because() => _effective = GenerationProtectionSchemas.For(1, [((EventTypeGeneration)1, _own), ((EventTypeGeneration)2, _higher)]);

    [Fact] void should_protect_each_item() => _effective.Properties["tokens"].Item.ActualSchema.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_not_protect_the_array_as_a_whole() => _effective.Properties["tokens"].GetSchemaMetadata(SchemaMetadataCategory.Compliance).ShouldBeEmpty();
    [Fact] void should_leave_the_registered_schema_unchanged() => _own.HasSchemaMetadata().ShouldBeFalse();
}
