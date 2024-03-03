// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Json;

/// <summary>
/// Represents a <see cref="JsonConverterFactory"/> for creating converters for types with object properties.
/// </summary>
/// <typeparam name="TConverter">Type of converter the factory is for.</typeparam>
/// <typeparam name="TTarget">Type of target the converter is for.</typeparam>
public class TypeWithObjectPropertiesJsonConverterFactory<TConverter, TTarget> : JsonConverterFactory
    where TConverter : TypeWithObjectPropertiesJsonConverter<TTarget>, new()
    where TTarget : class
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsAssignableTo(typeof(TTarget));

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        new TConverter();
}
