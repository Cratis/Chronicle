// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_registering_precise_defaulted_converter_schemas : given.a_json_schema_generator
{
    readonly Type[] _types = [typeof(Scalar), typeof(Collection), typeof(Duration), typeof(Enumeration), typeof(ObjectOverride), typeof(Character), typeof(Versioned)];
    bool[] _compatible;
    JsonSchema[] _precise;
    JsonSchema[] _legacy;

    void Because()
    {
        _legacy = _types.Select(_generator.GenerateLegacyEventType).ToArray();
        _precise = _types.Select(_generator.Generate).ToArray();
        _compatible = _legacy.Zip(_precise, (stored, incoming) => stored.IsCompatibleWith(incoming)).ToArray();
    }

    [Fact] void should_accept_scalar_concepts() => _compatible[0].ShouldBeTrue();
    [Fact] void should_accept_concept_collections() => _compatible[1].ShouldBeTrue();
    [Fact] void should_accept_timespan_concepts() => _compatible[2].ShouldBeTrue();
    [Fact] void should_accept_enum_concepts() => _compatible[3].ShouldBeTrue();
    [Fact] void should_accept_object_overrides() => _compatible[4].ShouldBeTrue();
    [Fact] void should_accept_character_concepts() => _compatible[5].ShouldBeTrue();
    [Fact] void should_accept_version_concepts() => _compatible[6].ShouldBeTrue();
    [Fact] void should_exercise_a_refinement_for_every_shape() => _legacy.Zip(_precise, (stored, incoming) => stored.ToJson() != incoming.ToJson()).ShouldEqual(Enumerable.Repeat(true, _types.Length));

    record Amount(decimal Value) : ConceptAs<decimal>(Value);
    record Label(string Value) : ConceptAs<string>(Value);
    record Elapsed(TimeSpan Value) : ConceptAs<TimeSpan>(Value);
    enum Choice
    {
        First = 0,
        Second = 1
    }
    record Selected(Choice Value) : ConceptAs<Choice>(Value);
    record Letter(char Value) : ConceptAs<char>(Value);
    record Revision(Version Value) : ConceptAs<Version>(Value);
    record Scalar(Amount? Value = null);
    record Collection(IReadOnlyList<Label>? Value = null);
    record Duration(Elapsed? Value = null);
    record Enumeration(Selected? Value = null);
    record Character(Letter? Value = null);
    record Versioned(Revision? Value = null);
    record ObjectOverride(Wrapped? Value = null);
    record Payload(string Name, Child Child);
    record Child(int Id);

    [JsonSchemaType(typeof(Payload))]
    [JsonConverter(typeof(WrappedConverter))]
    record Wrapped(Payload Value);

    class WrappedConverter : JsonConverter<Wrapped>
    {
        public override Wrapped Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(JsonSerializer.Deserialize<Payload>(ref reader, options)!);
        public override void Write(Utf8JsonWriter writer, Wrapped value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value.Value, options);
    }
}
