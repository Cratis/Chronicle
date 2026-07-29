// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Attribute used to override the type a type is represented as in the generated JSON schema.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonSchemaTypeAttribute"/> class.
/// <para>
/// A type that brings its own <c>JsonConverter</c> serializes to something other than its own shape — a value
/// object written as a single string, for instance. The generated schema is what Chronicle stores and reads
/// values against, so it has to describe what actually goes on the wire; without this the schema would describe
/// the CLR shape and the value would not round-trip. Adorn the type with this attribute to state what its
/// converter actually produces, rather than depending on attributes from whichever schema library is in use.
/// </para>
/// </remarks>
/// <param name="type">The <see cref="Type"/> the adorned type is represented as in the JSON schema.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class JsonSchemaTypeAttribute(Type type) : Attribute
{
    /// <summary>
    /// Gets the <see cref="Type"/> the adorned type is represented as in the JSON schema.
    /// </summary>
    public Type Type { get; } = type;
}
