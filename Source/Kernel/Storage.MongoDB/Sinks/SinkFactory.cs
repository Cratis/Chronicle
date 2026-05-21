// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
/// <remarks>
/// Marked with <see cref="IgnoreConventionAttribute"/> so convention binding does not register it
/// automatically. The MongoDB sink factory depends on MongoDB-specific services that are only wired
/// up by <c>WithMongoDB</c>; when both Storage.MongoDB and Storage.Sql assemblies are loaded (as in
/// the integration test project), letting convention binding pick up backend-specific factories
/// causes <c>IInstancesOf&lt;T&gt;</c> enumerations to fail for the inactive backend.
/// </remarks>
/// <param name="database"><see cref="IDatabase"/> for accessing MongoDB.</param>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between documents and <see cref="ExpandoObject"/>.</param>
[IgnoreConvention]
public class SinkFactory(
    IDatabase database,
    ITypeFormats typeFormats,
    IExpandoObjectConverter expandoObjectConverter) : ISinkFactory
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
    {
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
