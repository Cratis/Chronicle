// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_GenerationProtectionSchemas.when_resolving;

public class and_the_property_already_has_metadata : Specification
{
    JsonSchema _own;
    JsonSchema _higher;
    JsonSchema _effective;

    void Establish()
    {
        _own = JsonSchema.FromJson("""{"type":"object","properties":{"token":{"type":"string","security":[{"metadataType":"Encrypted","details":"own"}]}}}""");
        _higher = JsonSchema.FromJson("""{"type":"object","properties":{"token":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
    }

    void Because() => _effective = GenerationProtectionSchemas.For(1, [((EventTypeGeneration)1, _own), ((EventTypeGeneration)2, _higher)]);

    [Fact] void should_keep_the_registered_instance() => _effective.ShouldBeSame(_own);
    [Fact] void should_keep_the_own_classification() => _effective.Properties["token"].GetSchemaMetadata(SchemaMetadataCategory.Security).Single().details.ShouldEqual("own");
    [Fact] void should_not_mix_categories() => _effective.Properties["token"].GetSchemaMetadata(SchemaMetadataCategory.Compliance).ShouldBeEmpty();
}
