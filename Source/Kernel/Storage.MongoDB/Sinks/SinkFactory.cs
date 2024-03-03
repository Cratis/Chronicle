// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Kernel.Storage.Sinks;
using Cratis.Models;
using Cratis.Schemas;
using Cratis.Sinks;

namespace Cratis.Kernel.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
public class SinkFactory : ISinkFactory
{
    readonly IDatabase _database;
    readonly ITypeFormats _typeFormats;
    readonly IExpandoObjectConverter _expandoObjectConverter;

    /// <summary>
    /// /// Initializes a new instance of the <see cref="SinkFactory"/> class.
    /// </summary>
    /// <param name="database"><see cref="IDatabase"/> for accessing MongoDB.</param>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
    public SinkFactory(
        IDatabase database,
        ITypeFormats typeFormats,
        IExpandoObjectConverter expandoObjectConverter)
    {
        _database = database;
        _typeFormats = typeFormats;
        _expandoObjectConverter = expandoObjectConverter;
    }

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, Model model)
    {
        var mongoDBConverter = new MongoDBConverter(
            _expandoObjectConverter,
            _typeFormats,
            model);

        var mongoDBSinkCollections = new SinkCollections(
            model,
            _database.GetReadModelDatabase(eventStore, @namespace));
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
