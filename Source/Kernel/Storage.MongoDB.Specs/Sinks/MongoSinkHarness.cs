// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Runs the shared <see cref="ISink"/> contract against the MongoDB sink, in a database of its own.
/// </summary>
/// <remarks>
/// The fixture is a property rather than a constructor argument because the contract creates the harness
/// itself; a case needing the container overrides that and hands one over.
/// </remarks>
public class MongoSinkHarness : ISinkHarness
{
    IMongoClient? _client;
    string? _databaseName;

    /// <summary>
    /// Gets or sets the <see cref="MongoDBFixture"/> supplying the container.
    /// </summary>
    public MongoDBFixture? Fixture { get; set; }

    /// <inheritdoc/>
    public ISink CreateSink(ReadModelDefinition definition)
    {
        _databaseName = $"chronicle_sink_contract_{Guid.NewGuid():N}";
        _client = new MongoClient(Fixture!.ConnectionString);
        var database = _client.GetDatabase(_databaseName);

        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        var collections = new SinkCollections(definition, database);
        var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, definition, NullLogger<MongoDBConverter>.Instance);
        var changesetConverter = new ChangesetConverter(definition, mongoDBConverter, collections, expandoObjectConverter);

        return new Sink(definition, mongoDBConverter, collections, changesetConverter, expandoObjectConverter);
    }

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
