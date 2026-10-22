// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Runs the shared <see cref="ISink"/> contract against the MongoDB sink with every write fenced, in a database of its own.
/// </summary>
/// <remarks>
/// Fenced writes run in transactions, so the harness needs a replica set. The sink owns its generation through a claimed
/// <see cref="FenceToken"/>; a case revokes it through <see cref="Revoke"/>.
/// </remarks>
public class FencedMongoSinkHarness : ISinkHarness
{
    /// <summary>
    /// The materialization key the sink's gate is held under.
    /// </summary>
    public const string MaterializationKey = "store/namespace/test-read-model";

    IMongoClient? _client;
    string? _databaseName;
    FenceGates _gates = default!;

    /// <summary>
    /// Gets or sets the connection string of the replica set.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="IFencedBatchProbe"/> the sink's batches run through.
    /// </summary>
    public IFencedBatchProbe Probe { get; set; } = new NullFencedBatchProbe();

    /// <summary>
    /// Gets the <see cref="IMongoDatabase"/> the sink was created in.
    /// </summary>
    public IMongoDatabase Database { get; private set; } = default!;

    /// <summary>
    /// Gets the <see cref="FenceToken"/> the sink writes with.
    /// </summary>
    public FenceToken Token { get; private set; } = default!;

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition)
    {
        _databaseName = MongoDBSpecDatabaseNames.New();
        _client = new MongoClient(ConnectionString);
        Database = _client.GetDatabase(_databaseName);
        _gates = new FenceGates(Database);
        Token = _gates.Claim(MaterializationKey, definition.LatestGeneration.Value).GetAwaiter().GetResult();
        var writes = new FencedSinkWrites(new FixedFenceTokenSource(Token), new FencedBatchExecutor(new MongoFenceStore(Database), FenceRetryPolicy.Default, Probe));

        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        var collections = new SinkCollections(definition, Database);
        var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, definition, NullLogger<MongoDBConverter>.Instance);
        var changesetConverter = new ChangesetConverter(definition, mongoDBConverter, collections, expandoObjectConverter, NullLogger<ChangesetConverter>.Instance, writes);
        var changeStreams = new ReadModelChangeStreams(NullLogger<ReadModelChangeStreams>.Instance);

        return new Sink(definition, mongoDBConverter, collections, changesetConverter, expandoObjectConverter, changeStreams, NullLogger<Sink>.Instance, writes);
    }

    /// <summary>
    /// Revokes the sink's ownership of its generation.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task Revoke() => _gates.Revoke(Token.MaterializationKey, Token.Generation);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_client is not null && _databaseName is not null)
        {
            _client.DropDatabase(_databaseName);
            _client.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
