// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Diagnostics;
using Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
/// <remarks>
/// Takes <see cref="IServiceProvider"/> and resolves <see cref="IDatabase"/> and
/// <see cref="IExpandoObjectConverter"/> lazily on <see cref="CreateFor(EventStoreName, EventStoreNamespaceName, ReadModelDefinition)"/> rather than
/// eagerly via the constructor. This lets the factory be instantiated even in modes
/// where MongoDB is not the active backend (e.g. SQL OOP integration tests load both
/// Storage.MongoDB and Storage.Sql assemblies, and <c language="csharp">IInstancesOf&lt;ISinkFactory&gt;</c>
/// enumerates every implementation, while the DI cleanup that strips inactive backends
/// removes MongoDB-only services such as <see cref="IExpandoObjectConverter"/>).
/// Construction no longer fails, and the inactive backend's sink type simply never
/// appears in any read model definition that the active backend would process.
/// </remarks>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve <see cref="IDatabase"/> and <see cref="IExpandoObjectConverter"/> on demand.</param>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
public class SinkFactory(
    IServiceProvider serviceProvider,
    ITypeFormats typeFormats) : ISinkFactory
{
    readonly Lazy<NamedSinkDatabases> _namedDatabases = new(() => new(serviceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value));
    readonly Lazy<IExpandoObjectConverter> _expandoObjectConverter = new(() => new ExpandoObjectConverter(typeFormats));
    readonly Lazy<IReadModelChangeStreams> _changeStreams = new(() => new ReadModelChangeStreams(serviceProvider.GetRequiredService<ILogger<ReadModelChangeStreams>>()));

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) =>
        Create(eventStore, @namespace, readModel, serviceProvider.GetRequiredService<IDatabase>().GetReadModelDatabase(eventStore, @namespace));

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, SinkName sink) =>
        sink.IsDefault
            ? CreateFor(eventStore, @namespace, readModel)
            : Create(eventStore, @namespace, readModel, _namedDatabases.Value.GetReadModelDatabase(sink, eventStore, @namespace));

    Sink Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, IMongoDatabase readModelDatabase)
    {
        // A named sink stores to MongoDB whatever engine the kernel runs on, and the kernel removes the MongoDB storage services
        // when it runs on another one, so the collaborators a MongoDB sink needs fall back to instances of its own.
        var expandoObjectConverter = serviceProvider.GetService<IExpandoObjectConverter>() ?? _expandoObjectConverter.Value;

        var mongoDBConverter = new MongoDBConverter(
            expandoObjectConverter,
            typeFormats,
            readModel,
            serviceProvider.GetRequiredService<ILogger<MongoDBConverter>>());

        var writes = CreateWrites(eventStore, @namespace, readModel, readModelDatabase);
        var mongoDBSinkCollections = new SinkCollections(
            readModel,
            readModelDatabase,
            serviceProvider.GetRequiredService<ILogger<SinkCollections>>(),
            writes,
            new MongoDBGenerationContainerClaims(readModelDatabase));
        var mongoDBChangesetConverter = new ChangesetConverter(
            readModel,
            mongoDBConverter,
            mongoDBSinkCollections,
            expandoObjectConverter,
            serviceProvider.GetRequiredService<ILogger<ChangesetConverter>>(),
            writes);

        return new Sink(
            readModel,
            mongoDBConverter,
            mongoDBSinkCollections,
            mongoDBChangesetConverter,
            expandoObjectConverter,
            serviceProvider.GetService<IReadModelChangeStreams>() ?? _changeStreams.Value,
            serviceProvider.GetRequiredService<ILogger<Sink>>(),
            writes,
            serviceProvider.GetRequiredService<IStorageAuthorizationDiagnostics>());
    }

    ISinkWrites CreateWrites(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, IMongoDatabase readModelDatabase)
    {
        if (serviceProvider.GetService<IOptions<ChronicleOptions>>()?.Value.Features.ProjectionGenerations != true)
        {
            return UnfencedSinkWrites.Instance;
        }

        Task.Run(() => MongoTopology.RequireTransactions(readModelDatabase)).GetAwaiter().GetResult();
        var tokens = new ClaimingFenceTokenSource(new FenceGates(readModelDatabase), $"{eventStore}/{@namespace}/{readModel.Identifier}", readModel.LatestGeneration.Value);
        return new FencedSinkWrites(
            tokens,
            new FencedBatchExecutor(new MongoFenceStore(readModelDatabase)),
            serviceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value.ReadModels.MaxBatchSize);
    }
}
