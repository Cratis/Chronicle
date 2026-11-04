// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_GenerationProtectionSchemas.when_resolving;

public class and_a_higher_generation_classifies_the_same_property : for_ProtectedEventTypeMigrations.given.a_token_protected_only_in_the_latest_generation
{
    JsonSchema _effective;
    JsonSchema _cached;

    void Because()
    {
        _effective = GenerationProtectionSchemas.For(_definition, 1);
        _cached = GenerationProtectionSchemas.For(_definition, 1);
    }

    [Fact] void should_inherit_pii() => _effective.Properties["token"].GetSchemaMetadata(SchemaMetadataCategory.Compliance).Single().metadataType.ShouldEqual("PII");
    [Fact] void should_report_protection() => _effective.HasSchemaMetadata().ShouldBeTrue();
    [Fact] void should_leave_the_registered_schema_unchanged() => _schemas[0].HasSchemaMetadata().ShouldBeFalse();
    [Fact] void should_reuse_the_effective_schema() => _cached.ShouldBeSame(_effective);
}
