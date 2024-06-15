// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Models;
using Cratis.Schemas;
using Cratis.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
/// <remarks>
/// /// Initializes a new instance of the <see cref="SinkFactory"/> class.
/// </remarks>
/// <param name="database"><see cref="IDatabase"/> for accessing MongoDB.</param>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
public class SinkFactory(
    IDatabase database,
    ITypeFormats typeFormats,
    IExpandoObjectConverter expandoObjectConverter) : ISinkFactory
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, Model model)
    {
        var mongoDBConverter = new MongoDBConverter(
            expandoObjectConverter,
            typeFormats,
            model);

        var mongoDBSinkCollections = new SinkCollections(
            model,
            database.GetReadModelDatabase(eventStore, @namespace));
        var mongoDBChangesetConverter = new ChangesetConverter(
            model,
            mongoDBConverter,
            mongoDBSinkCollections,
            expandoObjectConverter);

        return new Sink(
            model,
            mongoDBConverter,
            mongoDBSinkCollections,
            mongoDBChangesetConverter,
            expandoObjectConverter);
    }
}
