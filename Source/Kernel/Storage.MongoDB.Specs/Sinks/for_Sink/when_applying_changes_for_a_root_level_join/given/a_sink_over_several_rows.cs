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
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join.given;

/// <summary>
/// A real MongoDB-backed <see cref="Sink"/> holding four already-materialized rows that differ only in the
/// joined-on column: one carrying the value the join is expected to match, one carrying a different value,
/// one where the column is explicitly null, and one where the column is absent entirely.
/// </summary>
/// <remarks>
/// The join writes through <c>UpdateMany</c>, so the failure mode worth specifying is write amplification: a
/// comparand that matches more documents than it should stamps every one of them, and a null comparand
/// matches both the null row and the absent row. Every spec built on this context therefore asserts on the
/// documents that must NOT change, comparing them whole against the state they were seeded in.
/// </remarks>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
public abstract class a_sink_over_several_rows(MongoDBFixture fixture) : IAsyncLifetime
{
    /// <summary>
    /// The value the join stamps onto every row it matches.
    /// </summary>
    public const string StampedValue = "stamped-by-the-join";

    /// <summary>
    /// The subject value carried by a blind root-level property change.
    /// </summary>
    public const string BlindSubject = "blind-root-subject";

    /// <summary>
    /// A regular root property included beside a join to prove all blind root updates are suppressed.
    /// </summary>
    public const string OrdinaryRootProperty = "ordinaryRootProperty";

    /// <summary>
    /// The value carried by the regular blind root property change.
    /// </summary>
    public const string OrdinaryRootValue = "blind-root-write";

    /// <summary>
    /// The property changed by the correctly filtered join payload.
    /// </summary>
    public const string StampedProperty = "stamped";

    /// <summary>
    /// The row whose joined-on column holds <see cref="MatchingValue"/>.
    /// </summary>
    public const string RowWithTheMatchingValue = "row-with-the-matching-value";

    /// <summary>
    /// The row whose joined-on column holds some other value.
    /// </summary>
    public const string RowWithAnotherValue = "row-with-another-value";

    /// <summary>
    /// The row whose joined-on column is explicitly null.
    /// </summary>
    public const string RowWithANullColumn = "row-with-a-null-column";

    /// <summary>
    /// The row that does not carry the joined-on column at all.
    /// </summary>
    public const string RowWithoutTheColumn = "row-without-the-column";

    const string JoinedOnProperty = "joinedOn";

    IMongoClient _client = default!;
    IMongoCollection<BsonDocument> _collection = default!;
    Sink _sink = default!;
    string _databaseName = default!;

    /// <summary>
    /// Gets the value the joined-on column holds on <see cref="RowWithTheMatchingValue"/>.
    /// </summary>
    public Guid MatchingValue { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets every stored document as it was seeded, by row identifier.
    /// </summary>
    public IDictionary<string, BsonDocument> DocumentsBeforeTheJoin { get; private set; } = new Dictionary<string, BsonDocument>();

    /// <summary>
    /// Gets every stored document as it stands after the join, by row identifier.
    /// </summary>
    public IDictionary<string, BsonDocument> DocumentsAfterTheJoin { get; private set; } = new Dictionary<string, BsonDocument>();

    /// <summary>
    /// Gets the error the sink raised while applying the join, if any.
    /// </summary>
    public Exception? Error { get; private set; }

    /// <summary>
    /// Gets the key the join arrives with.
    /// </summary>
    protected abstract object? JoinKey { get; }

    /// <summary>
    /// Gets whether the sink should apply the changes inside a bulk window.
    /// </summary>
    protected virtual bool UseBulkMode => false;

    /// <summary>
    /// Gets root property differences that have no document key target during a root-level join.
    /// </summary>
    protected virtual IReadOnlyCollection<PropertyDifference> RootPropertyDifferences => [];

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _databaseName = $"chr_join_rows_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);

        var schema = JsonSchema.FromType<GuidJoinedReadModel>();
        var readModel = CreateReadModelDefinition(schema);
        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        var collections = new SinkCollections(readModel, _client.GetDatabase(_databaseName));
        var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, readModel);
        var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, expandoObjectConverter);
        _sink = new Sink(readModel, mongoDBConverter, collections, changesetConverter, expandoObjectConverter);
        _collection = collections.GetCollection();

        await InsertRows();
        DocumentsBeforeTheJoin = await ReadRows();

        if (UseBulkMode)
        {
            await _sink.BeginBulk();
        }

        try
        {
            await _sink.ApplyChanges(
                new Key(JoinKey!, ArrayIndexers.NoIndexers),
                CreateJoinChangeset(),
                EventSequenceNumber.First);
        }
        catch (Exception error)
        {
            Error = error;
        }
        finally
        {
            if (UseBulkMode)
            {
                await _sink.EndBulk();
            }
        }

        DocumentsAfterTheJoin = await ReadRows();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

    /// <summary>
    /// Determine whether a row is unchanged by the join.
    /// </summary>
    /// <param name="rowId">Identifier of the row to check.</param>
    /// <returns>True when the stored document is identical to the one seeded.</returns>
    public bool IsUnchanged(string rowId) => DocumentsBeforeTheJoin[rowId].Equals(DocumentsAfterTheJoin[rowId]);

    /// <summary>
    /// Get the value the join stamps, read back from a stored document.
    /// </summary>
    /// <param name="rowId">Identifier of the row to read.</param>
    /// <returns>The stamped value.</returns>
    public string StampedOn(string rowId) => DocumentsAfterTheJoin[rowId][StampedProperty].AsString;

    static AppendedEvent CreateEvent()
    {
        var context = EventContext.From(
            "test-store",
            "test-namespace",
            EventType.Unknown,
            EventSourceType.Default,
            RowWithTheMatchingValue,
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            CorrelationId.NotSet);

        return new AppendedEvent(context, new ExpandoObject());
    }

    static ReadModelDefinition CreateReadModelDefinition(JsonSchema schema) =>
        new(
            typeof(GuidJoinedReadModel).FullName,
            $"rows_{Guid.NewGuid():N}",
            nameof(GuidJoinedReadModel),
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, schema }
            },
            []);

    async Task InsertRows() =>
        await _collection.InsertManyAsync(
        [
            new BsonDocument
            {
                ["_id"] = RowWithTheMatchingValue,
                [JoinedOnProperty] = new BsonBinaryData(MatchingValue, GuidRepresentation.Standard),
                [StampedProperty] = string.Empty,
                [OrdinaryRootProperty] = string.Empty
            },
            new BsonDocument
            {
                ["_id"] = RowWithAnotherValue,
                [JoinedOnProperty] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
                [StampedProperty] = string.Empty,
                [OrdinaryRootProperty] = string.Empty
            },
            new BsonDocument
            {
                ["_id"] = RowWithANullColumn,
                [JoinedOnProperty] = BsonNull.Value,
                [StampedProperty] = string.Empty,
                [OrdinaryRootProperty] = string.Empty
            },
            new BsonDocument
            {
                ["_id"] = RowWithoutTheColumn,
                [StampedProperty] = string.Empty,
                [OrdinaryRootProperty] = string.Empty
            }
        ]);

    async Task<IDictionary<string, BsonDocument>> ReadRows()
    {
        using var cursor = await _collection.FindAsync(Builders<BsonDocument>.Filter.Empty);
        var documents = await cursor.ToListAsync();
        return documents.ToDictionary(_ => _["_id"].AsString, _ => _);
    }

    IChangeset<AppendedEvent, ExpandoObject> CreateJoinChangeset()
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), CreateEvent(), new ExpandoObject());
        changeset.Add(new Joined(
            new ExpandoObject(),
            JoinKey!,
            new PropertyPath(JoinedOnProperty),
            ArrayIndexers.NoIndexers,
            [
                new PropertiesChanged<ExpandoObject>(
                    new ExpandoObject(),
                    [new PropertyDifference(new PropertyPath(StampedProperty), string.Empty, StampedValue)])
            ]));

        if (RootPropertyDifferences.Count != 0)
        {
            changeset.Add(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), RootPropertyDifferences));
        }

        return changeset;
    }
}
