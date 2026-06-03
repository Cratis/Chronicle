// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
/// <remarks>
/// Takes <see cref="IServiceProvider"/> and resolves <see cref="IDatabase"/> lazily on
/// <see cref="CreateFor"/> rather than eagerly via the constructor. This lets the factory
/// be instantiated even in modes where MongoDB is not the active backend (e.g. SQL OOP
/// integration tests load both Storage.MongoDB and Storage.Sql assemblies, and
/// <c>IInstancesOf&lt;ISinkFactory&gt;</c> enumerates every implementation). Construction
/// no longer fails, and the inactive backend's sink type simply never appears in any
/// read model definition that the active backend would process.
/// </remarks>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve <see cref="IDatabase"/> on demand.</param>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
public class SinkFactory(
    IServiceProvider serviceProvider,
    ITypeFormats typeFormats,
    IExpandoObjectConverter expandoObjectConverter) : ISinkFactory
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
    {
        var database = serviceProvider.GetRequiredService<IDatabase>();

        var mongoDBConverter = new MongoDBConverter(
            expandoObjectConverter,
            typeFormats,
            readModel);

        var mongoDBSinkCollections = new SinkCollections(
            readModel,
            database.GetReadModelDatabase(eventStore, @namespace));
        var mongoDBChangesetConverter = new ChangesetConverter(
            readModel,
            mongoDBConverter,
            mongoDBSinkCollections,
            expandoObjectConverter);

        return new Sink(
            readModel,
            mongoDBConverter,
            mongoDBSinkCollections,
            mongoDBChangesetConverter,
            expandoObjectConverter);
    }
}
