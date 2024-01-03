// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Kernel.Persistence.Sinks;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
public class SinkFactory : ISinkFactory
{
    readonly ITypeFormats _typeFormats;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ISinkDatabaseProvider _databaseProvider;

    /// <summary>
    /// /// Initializes a new instance of the <see cref="SinkFactory"/> class.
    /// </summary>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    /// <param name="databaseProvider">Provider for <see cref="ISinkCollections"/> to use.</param>
    public SinkFactory(
        ITypeFormats typeFormats,
        IExpandoObjectConverter expandoObjectConverter,
        ISinkDatabaseProvider databaseProvider)
    {
        _typeFormats = typeFormats;
        _expandoObjectConverter = expandoObjectConverter;
        _databaseProvider = databaseProvider;
    }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public ISink CreateFor(Model model)
    {
        var mongoDBConverter = new MongoDBConverter(
            _expandoObjectConverter,
            _typeFormats,
            model);

        var mongoDBSinkCollections = new SinkCollections(model, _databaseProvider);
        var mongoDBChangesetConverter = new ChangesetConverter(
            model,
            mongoDBConverter,
            mongoDBSinkCollections,
            _expandoObjectConverter);

        return new Sink(
            model,
            mongoDBConverter,
            mongoDBSinkCollections,
            mongoDBChangesetConverter,
            _expandoObjectConverter);
    }
}
