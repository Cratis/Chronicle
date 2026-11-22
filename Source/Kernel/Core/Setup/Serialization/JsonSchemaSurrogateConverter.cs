// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between a <see cref="JsonSchema"/> and its <see cref="JsonSchemaSurrogate"/>.
/// </summary>
[RegisterConverter]
public sealed class JsonSchemaSurrogateConverter : IConverter<JsonSchema, JsonSchemaSurrogate>
{
    /// <inheritdoc/>
    public JsonSchema ConvertFromSurrogate(in JsonSchemaSurrogate surrogate) => JsonSchema.FromJson(surrogate.Json);

    /// <inheritdoc/>
    public JsonSchemaSurrogate ConvertToSurrogate(in JsonSchema value) => new(value.ToJson());
}
