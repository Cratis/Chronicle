// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
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
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.MongoDB;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) =>
        CreateSink(eventStore, @namespace, readModel, null);

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, GenerationSinkContext context)
    {
        var model = context.GetReadModel(eventStore, @namespace);
        if (model.Sink.Type != TypeId) throw new Cratis.Chronicle.Storage.Generations.Bindings.MaterializationBindingsNotSupported();
        return CreateSink(eventStore, @namespace, model, context);
    }

    static FenceToken Token(GenerationSinkContext context) => new(context.Binding.Key.Value, context.Binding.Identity.Generation.Value, context.Grant.Epoch);

    Sink CreateSink(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, GenerationSinkContext? context)
    {
        var database = serviceProvider.GetRequiredService<IDatabase>();
        var expandoObjectConverter = serviceProvider.GetRequiredService<IExpandoObjectConverter>();

        var mongoDBConverter = new MongoDBConverter(
            expandoObjectConverter,
            typeFormats,
            readModel,
            serviceProvider.GetRequiredService<ILogger<MongoDBConverter>>());

        var readModelDatabase = database.GetReadModelDatabase(eventStore, @namespace);
        var writes = CreateWrites(eventStore, @namespace, readModel, readModelDatabase, context);
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
            serviceProvider.GetRequiredService<IReadModelChangeStreams>(),
            serviceProvider.GetRequiredService<ILogger<Sink>>(),
            writes)
        {
            GenerationIndexDdl = context is null ? null : operation => new EpochCheckedDdl(new FenceGates(readModelDatabase)).Run(Token(context), operation)
        };
    }

    ISinkWrites CreateWrites(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, IMongoDatabase readModelDatabase, GenerationSinkContext? context)
    {
        // A generation writer always writes under the fixed grant it was created with; it never claims a token of its own.
        if (context is not null)
        {
            Task.Run(() => MongoTopology.RequireTransactions(readModelDatabase)).GetAwaiter().GetResult();
            return new FencedSinkWrites(
                new FixedFenceTokenSource(Token(context)),
                new FencedBatchExecutor(new MongoFenceStore(readModelDatabase)),
                serviceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value.ReadModels.MaxBatchSize);
        }

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
