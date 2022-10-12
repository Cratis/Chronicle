// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Serialization;
using MongoDB.Bson.Serialization;

namespace Aksio.Cratis.Extensions.MongoDB;

/// <summary>
/// Represents a <see cref="IBsonSerializationProvider"/> for supporting derived types.
/// </summary>
public class DerivedTypeSerializerProvider : IBsonSerializationProvider
{
    readonly IDerivedTypes _derivedTypes;
    readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DerivedTypeSerializerProvider"/> class.
    /// </summary>
    /// <param name="derivedTypes"><see cref="IDerivedTypes"/> in the system.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/>.</param>
    public DerivedTypeSerializerProvider(IDerivedTypes derivedTypes, JsonSerializerOptions options)
    {
        _derivedTypes = derivedTypes;
        _options = options;
    }

    /// <summary>
    /// Create a serializer instance for a specific type.
    /// </summary>
    /// <typeparam name="T">Type to create serializer for.</typeparam>
    /// <returns>New serializer instance.</returns>
    public DerivedTypeSerializer<T> CreateSerializer<T>()
    {
        return new DerivedTypeSerializer<T>(_options);
    }

    /// <inheritdoc/>
    public IBsonSerializer GetSerializer(Type type)
    {
        if (_derivedTypes.IsDerivedType(type))
        {
            var createSerializerGenericMethod = GetType().GetMethod(nameof(DerivedTypeSerializerProvider.CreateSerializer))!.MakeGenericMethod(type);
            return (createSerializerGenericMethod.Invoke(this, Array.Empty<object>()) as IBsonSerializer)!;
        }

        return null!;
    }
}
