// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents a JSON object that can not change after it was created.
/// </summary>
/// <remarks>
/// A <see cref="JsonObject"/> is a mutable tree, so a definition holding one can not be handed to another grain by reference. This holds the object as its
/// JSON text and gives every caller a tree of its own, so nobody can change what another caller sees.
/// </remarks>
/// <param name="Json">The JSON text of the object.</param>
[Immutable]
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Projections.Definitions.ImmutableJsonObject")]
[JsonConverter(typeof(ImmutableJsonObjectJsonConverter))]
public sealed record ImmutableJsonObject([property: Id(0)] string Json)
{
    /// <summary>
    /// Gets the empty object.
    /// </summary>
    public static readonly ImmutableJsonObject Empty = new("{}");

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableJsonObject"/> class holding the empty object.
    /// </summary>
    public ImmutableJsonObject()
        : this("{}")
    {
    }

    /// <summary>
    /// Implicitly convert a <see cref="JsonObject"/> to an <see cref="ImmutableJsonObject"/>, taking a snapshot of it.
    /// </summary>
    /// <param name="value">The <see cref="JsonObject"/> to take a snapshot of.</param>
    public static implicit operator ImmutableJsonObject(JsonObject value) => From(value);

    /// <summary>
    /// Take a snapshot of a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="value">The <see cref="JsonObject"/> to take a snapshot of.</param>
    /// <returns>The <see cref="ImmutableJsonObject"/>.</returns>
    public static ImmutableJsonObject From(JsonObject? value) => value is null ? Empty : new(value.ToJsonString());

    /// <summary>
    /// Gets a new <see cref="JsonObject"/> tree of the object. Every call returns a tree of its own that the caller is free to change.
    /// </summary>
    /// <returns>A new <see cref="JsonObject"/>.</returns>
    public JsonObject ToJsonObject() => JsonNode.Parse(Json)?.AsObject() ?? [];

    /// <summary>
    /// Gets the JSON text of the object.
    /// </summary>
    /// <returns>The JSON text.</returns>
    public string ToJsonString() => Json;

    /// <inheritdoc/>
    public override string ToString() => Json;
}
