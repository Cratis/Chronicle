// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadInventory.given;

public static class payload_types
{
    public interface IFixtureGrain : IGrainWithStringKey
    {
        Task<Payload> Read(Generated value);
        Task<Child[]> Children(Child[] children);
    }

    public record Payload
    {
        public required string DisplayName { get; init; }
        public required Id Id { get; init; }
        public Status Status { get; init; }
        public required IEnumerable<Child> Children { get; init; }
        public required Dictionary<string, Child> Lookup { get; init; }
        public required AbstractPayload Abstract { get; init; }
        public required Cycle Cycle { get; init; }
        public required Custom Custom { get; init; }
        public required IFixtureGrain Grain { get; init; }
        public required Exception Error { get; init; }
        [JsonIgnore] public string Ignored => string.Empty;
        [JsonPropertyName("renamed")] public string OriginalName => string.Empty;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Conditional => null;
    }

    [GenerateSerializer]
    [Alias("FixtureGenerated")]
    public class Generated
    {
        [Id(0)] readonly int _count = 3;
        [Id(1)] public string Name { get; set; } = string.Empty;
        public int Count => _count;
    }

    public record Id(string Value) : ConceptAs<string>(Value);
    public enum Status
    {
        Active = 1,
        Paused = 2
    }
    public record Child(string Name);
    public abstract record AbstractPayload;
    public record ConcretePayload(string Value) : AbstractPayload;
    public record Cycle(Payload Parent);
    public record Custom(string Hidden);

    public class Converter : JsonConverter<Custom>
    {
        public override Custom Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(string.Empty);
        public override void Write(Utf8JsonWriter writer, Custom value, JsonSerializerOptions options) => writer.WriteStringValue(value.Hidden);
    }
}
