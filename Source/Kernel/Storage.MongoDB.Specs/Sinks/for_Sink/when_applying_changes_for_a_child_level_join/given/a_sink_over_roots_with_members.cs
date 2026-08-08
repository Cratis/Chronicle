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

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_child_level_join.given;

/// <summary>
/// A real MongoDB-backed <see cref="Sink"/> holding three already-materialized roots, two of which carry the
/// member the child join identifies and one of which does not.
/// </summary>
/// <remarks>
/// The child branch of the join filter matches every root that holds the child, so it too writes through
/// UpdateMany and can amplify. It converted its comparand through the schema before the root branch did, and
/// it now shares the root branch's non-throwing fallback, so it needs the same behavioral guard: the right
/// members in the right roots stamped, and everything else byte-identical.
/// </remarks>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
public abstract class a_sink_over_roots_with_members(MongoDBFixture fixture) : IAsyncLifetime
{
    /// <summary>
    /// The value the join stamps onto every member it matches.
    /// </summary>
    public const string StampedValue = "stamped-by-the-child-join";

    /// <summary>
    /// A root holding the identified member alongside another one.
    /// </summary>
    public const string RootHoldingTheMember = "root-holding-the-member";

    /// <summary>
    /// A second root holding the identified member, so the update is specified to reach every root.
    /// </summary>
    public const string SecondRootHoldingTheMember = "second-root-holding-the-member";

    /// <summary>
    /// A root holding only members the join does not identify.
    /// </summary>
    public const string RootWithoutTheMember = "root-without-the-member";

    const string MembersProperty = "Members";
    const string MemberIdProperty = "MemberId";
    const string StampedProperty = "Stamped";

    IMongoClient _client = default!;
    IMongoCollection<BsonDocument> _collection = default!;
    Sink _sink = default!;
    string _databaseName = default!;

    /// <summary>
    /// Gets the identifier of the member the join is declared for.
    /// </summary>
    public Guid MemberId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the identifier of a member the join is not declared for.
    /// </summary>
    public Guid OtherMemberId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets every stored document as it was seeded, by root identifier.
    /// </summary>
    public IDictionary<string, BsonDocument> DocumentsBeforeTheJoin { get; private set; } = new Dictionary<string, BsonDocument>();

    /// <summary>
    /// Gets every stored document as it stands after the join, by root identifier.
    /// </summary>
    public IDictionary<string, BsonDocument> DocumentsAfterTheJoin { get; private set; } = new Dictionary<string, BsonDocument>();

    /// <summary>
    /// Gets the error the sink raised while applying the join, if any.
    /// </summary>
    public Exception? Error { get; private set; }

    /// <summary>
    /// Gets the identifier the child join arrives with.
    /// </summary>
    protected abstract object? JoinIdentifier { get; }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _databaseName = $"chr_child_join_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);

        var schema = JsonSchema.FromType<ChildJoinedReadModel>();
        var readModel = CreateReadModelDefinition(schema);
        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        var collections = new SinkCollections(readModel, _client.GetDatabase(_databaseName));
        var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, readModel);
        var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, expandoObjectConverter);
        _sink = new Sink(readModel, mongoDBConverter, collections, changesetConverter, expandoObjectConverter);
        _collection = collections.GetCollection();

        await InsertRoots();
        DocumentsBeforeTheJoin = await ReadRoots();

        var arrayIndexers = new ArrayIndexers([new ArrayIndexer($"[{MembersProperty}]", MemberIdProperty, JoinIdentifier!)]);

        try
        {
            await _sink.ApplyChanges(
                new Key(RootHoldingTheMember, arrayIndexers),
                CreateJoinChangeset(arrayIndexers),
                EventSequenceNumber.First);
        }
        catch (Exception error)
        {
            Error = error;
        }

        DocumentsAfterTheJoin = await ReadRoots();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

    /// <summary>
    /// Determine whether a root is unchanged by the join.
    /// </summary>
    /// <param name="rootId">Identifier of the root to check.</param>
    /// <returns>True when the stored document is identical to the one seeded.</returns>
    public bool IsUnchanged(string rootId) => DocumentsBeforeTheJoin[rootId].Equals(DocumentsAfterTheJoin[rootId]);

    /// <summary>
    /// Get the value the join stamps, read back from a member of a stored root.
    /// </summary>
    /// <param name="rootId">Identifier of the root to read.</param>
    /// <param name="memberId">Identifier of the member to read.</param>
    /// <returns>The stamped value.</returns>
    public string StampedOn(string rootId, Guid memberId) =>
        DocumentsAfterTheJoin[rootId][MembersProperty].AsBsonArray
            .Select(_ => _.AsBsonDocument)
            .Single(_ => _[MemberIdProperty].AsGuid == memberId)[StampedProperty]
            .AsString;

    static AppendedEvent CreateEvent()
    {
        var context = EventContext.From(
            "test-store",
            "test-namespace",
            EventType.Unknown,
            EventSourceType.Default,
            RootHoldingTheMember,
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            CorrelationId.NotSet);

        return new AppendedEvent(context, new ExpandoObject());
    }

    static ReadModelDefinition CreateReadModelDefinition(JsonSchema schema) =>
        new(
            typeof(ChildJoinedReadModel).FullName,
            $"roots_{Guid.NewGuid():N}",
            nameof(ChildJoinedReadModel),
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

    static BsonDocument Member(Guid memberId) =>
        new()
        {
            [MemberIdProperty] = new BsonBinaryData(memberId, GuidRepresentation.Standard),
            [StampedProperty] = string.Empty
        };

    static IChangeset<AppendedEvent, ExpandoObject> CreateJoinChangeset(ArrayIndexers arrayIndexers)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), CreateEvent(), new ExpandoObject());
        changeset.Add(new Joined(
            new ExpandoObject(),
            RootHoldingTheMember,
            new PropertyPath(MemberIdProperty),
            arrayIndexers,
            [
                new PropertiesChanged<ExpandoObject>(
                    new ExpandoObject(),
                    [
                        new PropertyDifference(
                            new PropertyPath($"[{MembersProperty}].{StampedProperty}"),
                            string.Empty,
                            StampedValue,
                            arrayIndexers)
                    ])
            ]));

        return changeset;
    }

    async Task InsertRoots() =>
        await _collection.InsertManyAsync(
        [
            new BsonDocument
            {
                ["_id"] = RootHoldingTheMember,
                [MembersProperty] = new BsonArray([Member(MemberId), Member(OtherMemberId)])
            },
            new BsonDocument
            {
                ["_id"] = SecondRootHoldingTheMember,
                [MembersProperty] = new BsonArray([Member(MemberId)])
            },
            new BsonDocument
            {
                ["_id"] = RootWithoutTheMember,
                [MembersProperty] = new BsonArray([Member(OtherMemberId)])
            }
        ]);

    async Task<IDictionary<string, BsonDocument>> ReadRoots()
    {
        using var cursor = await _collection.FindAsync(Builders<BsonDocument>.Filter.Empty);
        var documents = await cursor.ToListAsync();
        return documents.ToDictionary(_ => _["_id"].AsString, _ => _);
    }
}
