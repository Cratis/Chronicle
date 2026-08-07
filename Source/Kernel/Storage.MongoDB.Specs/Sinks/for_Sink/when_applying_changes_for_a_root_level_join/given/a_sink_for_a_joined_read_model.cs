// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;

/// <summary>
/// A real MongoDB-backed <see cref="Sink"/> holding one already-materialized row, onto which a root-level
/// join arrives afterwards — the forward path, where no row-creation-time backfill can hide the write.
/// The join always arrives keyed by the raw event source id string, which is what the projection engine
/// resolves for a root-level join; only the stored type of the joined-on column varies between the specs.
/// </summary>
/// <typeparam name="TReadModel">Type of read model the sink is for.</typeparam>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
public abstract class a_sink_for_a_joined_read_model<TReadModel>(MongoDBFixture fixture) : IAsyncLifetime
{
    /// <summary>
    /// The value the join stamps onto the row it matches.
    /// </summary>
    public const string StampedValue = "stamped-by-the-join";

    const string RowKey = "row-1";
    const string JoinedOnProperty = "joinedOn";
    const string StampedProperty = "stamped";

    IMongoClient _client = default!;
    IMongoCollection<BsonDocument> _collection = default!;
    IExpandoObjectConverter _expandoObjectConverter = default!;
    IMongoDBConverter _mongoDBConverter = default!;
    Sink _sink = default!;
    JsonSchema _schema = default!;
    string _databaseName = default!;

    /// <summary>
    /// Gets the <see cref="BsonType"/> the joined-on column is actually stored as.
    /// </summary>
    public BsonType StoredJoinedOnType { get; private set; }

    /// <summary>
    /// Gets the value of the property the join stamps, read back from the stored document.
    /// </summary>
    public string StampedAfterJoin { get; private set; } = default!;

    /// <summary>
    /// Gets the error the sink raised while applying the join, if any.
    /// </summary>
    public Exception? Error { get; private set; }

    /// <summary>
    /// Gets the value the joined-on column holds on the existing row.
    /// </summary>
    protected abstract object JoinedOnValue { get; }

    /// <summary>
    /// Gets the raw event source id string the join arrives with.
    /// </summary>
    protected abstract string JoinKey { get; }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _databaseName = $"chronicle_root_join_specs_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        _schema = JsonSchema.FromType<TReadModel>();

        var readModel = CreateReadModelDefinition();
        var typeFormats = new TypeFormats();
        _expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        var collections = new SinkCollections(readModel, _client.GetDatabase(_databaseName));
        _mongoDBConverter = new MongoDBConverter(_expandoObjectConverter, typeFormats, readModel);
        var changesetConverter = new ChangesetConverter(readModel, _mongoDBConverter, collections, _expandoObjectConverter, NullLogger<ChangesetConverter>.Instance);
        _sink = new Sink(readModel, _mongoDBConverter, collections, changesetConverter, _expandoObjectConverter);
        _collection = collections.GetCollection();

        await InsertExistingRow();
        StoredJoinedOnType = (await ReadRow())[JoinedOnProperty].BsonType;

        try
        {
            await _sink.ApplyChanges(
                new Key(JoinKey, ArrayIndexers.NoIndexers),
                CreateJoinChangeset(),
                EventSequenceNumber.First);
        }
        catch (Exception error)
        {
            Error = error;
        }

        StampedAfterJoin = (await ReadRow())[StampedProperty].AsString;
    }

    /// <inheritdoc/>
    public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

    static AppendedEvent CreateEvent()
    {
        var context = EventContext.From(
            "test-store",
            "test-namespace",
            EventType.Unknown,
            EventSourceType.Default,
            RowKey,
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            CorrelationId.NotSet);

        return new AppendedEvent(context, new ExpandoObject());
    }

    async Task InsertExistingRow()
    {
        dynamic row = new ExpandoObject();
        row.id = RowKey;
        row.joinedOn = JoinedOnValue;
        row.stamped = string.Empty;

        var document = _expandoObjectConverter.ToBsonDocument(row, _schema);
        document["_id"] = _mongoDBConverter.ToBsonValue(new Key(RowKey, ArrayIndexers.NoIndexers));
        await _collection.InsertOneAsync(document);
    }

    async Task<BsonDocument> ReadRow()
    {
        using var cursor = await _collection.FindAsync(Builders<BsonDocument>.Filter.Empty);
        return await cursor.SingleAsync();
    }

    IChangeset<AppendedEvent, ExpandoObject> CreateJoinChangeset()
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), CreateEvent(), new ExpandoObject());
        changeset.Add(new Joined(
            new ExpandoObject(),
            JoinKey,
            new PropertyPath(JoinedOnProperty),
            ArrayIndexers.NoIndexers,
            [
                new PropertiesChanged<ExpandoObject>(
                    new ExpandoObject(),
                    [new PropertyDifference(new PropertyPath(StampedProperty), string.Empty, StampedValue)])
            ]));

        return changeset;
    }

    ReadModelDefinition CreateReadModelDefinition() =>
        new(
            typeof(TReadModel).FullName,
            $"rows_{Guid.NewGuid():N}",
            typeof(TReadModel).Name,
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, _schema }
            },
            []);
}
