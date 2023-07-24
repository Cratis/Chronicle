// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Schemas;
using MongoDB.Bson;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBConverterFactory"/>.
/// </summary>
public class MongoDBConverterFactory : IMongoDBConverterFactory
{
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ITypeFormats _typeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBConverterFactory"/> class.
    /// </summary>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> to convert between <see cref="ExpandoObject"/> to <see cref="BsonDocument"/>.</param>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
    public MongoDBConverterFactory(
        IExpandoObjectConverter expandoObjectConverter,
        ITypeFormats typeFormats)
    {
        _expandoObjectConverter = expandoObjectConverter;
        _typeFormats = typeFormats;
    }

    /// <inheritdoc/>
    public IMongoDBConverter CreateFor(Model model) => new MongoDBConverter(_expandoObjectConverter, _typeFormats, model);
}
