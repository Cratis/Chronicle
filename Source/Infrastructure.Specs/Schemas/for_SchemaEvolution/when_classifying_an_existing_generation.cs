// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_SchemaEvolution;

public class when_classifying_an_existing_generation : Specification
{
    const string Stored = """{"type":"object","properties":{"name":{"type":"string"}}}""";

    [Theory]
    [InlineData(Stored, SchemaEvolutionKind.Identical)]
    [InlineData("""{"type":"object","properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""", SchemaEvolutionKind.Additive)]
    [InlineData("""{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""", SchemaEvolutionKind.Breaking)]
    [InlineData("""{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":"integer","default":0}}}""", SchemaEvolutionKind.Breaking)]
    [InlineData("""{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":["integer","null"]}}}""", SchemaEvolutionKind.Additive)]
    [InlineData("""{"type":"object","properties":{"name":{"type":"string","description":"annotation"}}}""", SchemaEvolutionKind.Identical)]
    [InlineData("""{"type":"object","properties":{"name":{"type":["string","null"]}}}""", SchemaEvolutionKind.Breaking)]
    [InlineData("""{"type":"object","properties":{}}""", SchemaEvolutionKind.Subset)]
    [InlineData("""{"type":"object","properties":{"name":{"type":"integer"}}}""", SchemaEvolutionKind.Breaking)]
    [InlineData("""{"type":"object","required":["name"],"properties":{"name":{"type":"string"}}}""", SchemaEvolutionKind.Breaking)]
    [InlineData("""{"type":"object","properties":{"name":{"type":"string","maxLength":5},"age":{"type":"integer"}}}""", SchemaEvolutionKind.Breaking)]
    [InlineData("""{"type":"object","additionalProperties":false,"properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""", SchemaEvolutionKind.Breaking)]
    public void should_classify_the_change(string incoming, SchemaEvolutionKind expected) =>
        SchemaEvolution.Classify(JsonSchema.FromJson(Stored), JsonSchema.FromJson(incoming)).ShouldEqual(expected);

    [Fact]
    void should_recognize_an_older_client() => SchemaEvolution.Classify(
        JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string"},"age":{"type":"integer"}}}"""),
        JsonSchema.FromJson(Stored)).ShouldEqual(SchemaEvolutionKind.Subset);

    [Theory]
    [InlineData("""{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":["integer","null"]}}}""")]
    [InlineData("""{"type":"object","required":["age"],"properties":{"name":{"type":"string"},"age":{"type":"integer","default":0}}}""")]
    public void should_recognize_an_older_client_after_a_required_nullable_or_defaulted_addition(string stored) => SchemaEvolution.Classify(
        JsonSchema.FromJson(stored), JsonSchema.FromJson(Stored)).ShouldEqual(SchemaEvolutionKind.Subset);

    [Fact]
    void should_reject_missing_protection_on_a_subset() => SchemaEvolution.Classify(
        JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string","security":"protected"},"age":{"type":"integer"}}}"""),
        JsonSchema.FromJson(Stored)).ShouldEqual(SchemaEvolutionKind.Breaking);

    [Fact]
    void should_tolerate_nullable_format_markers() => SchemaEvolution.Classify(
        JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string","format":"uuid"}}}"""),
        JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string","format":"uuid?"}}}""")).ShouldEqual(SchemaEvolutionKind.Tolerated);

    [Fact]
    void should_check_referenced_definitions() => SchemaEvolution.Classify(
        JsonSchema.FromJson("""{"type":"object","properties":{"name":{"$ref":"#/$defs/name"}},"$defs":{"name":{"type":"string"}}}"""),
        JsonSchema.FromJson("""{"type":"object","properties":{"name":{"$ref":"#/$defs/name"},"age":{"type":"integer"}},"$defs":{"name":{"type":"integer"}}}""")).ShouldEqual(SchemaEvolutionKind.Breaking);

    [Fact]
    void should_check_nested_properties() => SchemaEvolution.Classify(
        JsonSchema.FromJson("""{"type":"object","properties":{"address":{"type":"object","properties":{"city":{"type":"string"}}}}}"""),
        JsonSchema.FromJson("""{"type":"object","properties":{"address":{"type":"object","properties":{"city":{"type":"string"},"street":{"type":"string"}}}}}""")).ShouldEqual(SchemaEvolutionKind.Additive);
}
