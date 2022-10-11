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

    public DerivedTypeSerializerProvider(IDerivedTypes derivedTypes, JsonSerializerOptions options)
    {
        _derivedTypes = derivedTypes;
        _options = options;
    }

    /// <inheritdoc/>
    public DerivedTypeSerializer<T> CreateSerializer<T>()
    {
        return new DerivedTypeSerializer<T>(_options);
    }

    /// <inheritdoc/>
    public IBsonSerializer GetSerializer(Type type)
    {
        if( _derivedTypes.IsDerivedType(type))
        {
            var createSerializerGenericMethod = GetType().GetMethod(nameof(DerivedTypeSerializerProvider.CreateSerializer))!.MakeGenericMethod(type);
            return (createSerializerGenericMethod.Invoke(this, Array.Empty<object>()) as IBsonSerializer)!;
        }

        return null!;
    }
}
