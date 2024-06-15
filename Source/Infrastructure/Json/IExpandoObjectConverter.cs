// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using NJsonSchema;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Defines a converter that can convert between a <see cref="JsonObject"/> and a <see cref="ExpandoObject"/> with a <see cref="JsonSchema"/> holding the type information.
/// </summary>
public interface IExpandoObjectConverter
{
    /// <summary>
    /// Convert a <see cref="JsonObject"/> to <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="document"><see cref="JsonObject"/> to convert.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> with the type information.</param>
    /// <returns>A new <see cref="ExpandoObject"/> instance.</returns>
    ExpandoObject ToExpandoObject(JsonObject document, JsonSchema schema);

    /// <summary>
    /// Convert a <see cref="ExpandoObject"/> to <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="expandoObject">The <see cref="ExpandoObject"/> to convert.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> with the type information.</param>
    /// <returns>A new <see cref="JsonObject"/> instance.</returns>
    JsonObject ToJsonObject(ExpandoObject expandoObject, JsonSchema schema);
}
