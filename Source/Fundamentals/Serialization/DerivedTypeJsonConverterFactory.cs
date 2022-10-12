// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aksio.Cratis.Serialization;

/// <summary>
/// Represents a <see cref="JsonConverterFactory"/> for creating converters for types that are adorned with <see cref="DerivedTypeAttribute"/>.
/// </summary>
public class DerivedTypeJsonConverterFactory : JsonConverterFactory
{
    readonly IDerivedTypes _derivedTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="DerivedTypeJsonConverterFactory"/> class.
    /// </summary>
    /// <param name="derivedTypes"><see cref="IDerivedTypes"/> to use for discovering correct type.</param>
    public DerivedTypeJsonConverterFactory(IDerivedTypes derivedTypes)
    {
        _derivedTypes = derivedTypes;
    }

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => _derivedTypes.HasDerivatives(typeToConvert);

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        Activator.CreateInstance(typeof(DerivedTypeJsonConverter<>).MakeGenericType(typeToConvert), _derivedTypes) as JsonConverter;
}
