// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_GenerationProtectionSchemas.when_resolving;

public class and_the_property_is_nested : Specification
{
    JsonSchema _own;
    JsonSchema _higher;
    JsonSchema _effective;

    void Establish()
    {
        _own = JsonSchema.FromJson("""{"type":"object","properties":{"nested":{"type":"object","properties":{"token":{"type":"string"}}},"items":{"type":"array","items":{"type":"object","properties":{"token":{"type":"string"}}}}}}""");
        _higher = JsonSchema.FromJson("""{"type":"object","properties":{"nested":{"type":"object","properties":{"token":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}},"items":{"type":"array","items":{"type":"object","properties":{"token":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}}}}""");
    }

    void Because() => _effective = GenerationProtectionSchemas.For(1, [((EventTypeGeneration)1, _own), ((EventTypeGeneration)2, _higher)]);

    [Fact] void should_protect_the_nested_member() => _effective.Properties["nested"].ActualTypeSchema.Properties["token"].HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_protect_members_of_array_items() => _effective.Properties["items"].Item.ActualSchema.Properties["token"].HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_leave_the_registered_schema_unchanged() => _own.HasSchemaMetadata().ShouldBeFalse();
}
