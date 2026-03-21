// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents a JSON Schema property, extending <see cref="JsonSchema"/> with property-specific metadata.
/// </summary>
public class JsonSchemaProperty : JsonSchema
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaProperty"/> class (for object initializer usage).
    /// </summary>
    public JsonSchemaProperty() : base()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaProperty"/> class from a JSON object.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="node">The JSON object for this property's schema.</param>
    /// <param name="root">The root schema for $ref resolution.</param>
    public JsonSchemaProperty(string name, JsonObject node, JsonSchema root) : base(node, root)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the actual resolved schema for this property (follows $ref and allOf).
    /// Equivalent to <see cref="JsonSchema.ActualTypeSchema"/>.
    /// </summary>
    public JsonSchema ActualSchema => ActualTypeSchema;

    /// <summary>
    /// Gets whether any of the OneOf schemas has a $ref.
    /// </summary>
    public bool HasOneOfSchemaReference => OneOf.Any(s => s.HasReference);
}
