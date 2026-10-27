// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.given;

/// <summary>
/// An event sequence in the pre-upgrade schema: no Revisions column, holding a plain event and two events that
/// were revised in place by the old implementation. The provider is chosen by the concrete subclass.
/// </summary>
public abstract class a_sequence_with_legacy_revisions : Specification
{
    protected const string TableName = "event-sequence";
    protected static readonly EventType FirstGeneration = new("e4b1c3a0-5b0e-4f4e-9a55-7a1d3a6e9c12", EventTypeGeneration.First);
    protected static readonly EventType SecondGeneration = new(FirstGeneration.Id, new EventTypeGeneration(2));
    protected static readonly EventSourceId EventSource = "legacy-revisions-source";
    protected static readonly IdentityId AliceId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    protected const ulong PlainEvent = 0;
    protected const ulong EventRevisedAtGenerationOne = 1;
    protected const ulong EventRevisedAtGenerationTwo = 2;

    protected EventSequenceMigrator _migrator;
    protected IEventSequenceStorage _storage;
    protected string _connectionString;

    IIdentityStorage _identities;
    IEventTypesStorage _eventTypes;

    protected abstract Task<string> CreateDatabase();

    protected abstract void UseProvider(DbContextOptionsBuilder<EventSequenceDbContext> builder, string connectionString);

    protected virtual Task DropDatabase(string connectionString) => Task.CompletedTask;

    protected EventSequenceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>();
        UseProvider(builder, _connectionString);
        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        return new EventSequenceDbContext(builder.Options, TableName, _migrator);
    }

    protected static string Json(ExpandoObject content) => JsonSerializer.SerializeToNode(content)!.ToJsonString();

    protected static void SameJson(string actual, string expectedValue)
    {
        actual.ShouldNotEqual(string.Empty);
        JsonNode.DeepEquals(JsonNode.Parse(actual), JsonNode.Parse(Json(Content(expectedValue)))).ShouldBeTrue();
    }

    protected static ExpandoObject Content(string value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["value"] = value;
        return content;
    }

    protected Task Revise(ulong sequenceNumber, EventType eventType, string value, string hash) =>
        _storage.Revise(
            sequenceNumber,
            eventType,
            CorrelationId.New(),
            [new Causation(DateTimeOffset.UtcNow, CausationType.Unknown, new Dictionary<string, string> { ["reason"] = value })],
            [AliceId],
            DateTimeOffset.UtcNow,
            Content(value),
            hash);

    async Task Establish()
    {
        _connectionString = await CreateDatabase();
        _migrator = new EventSequenceMigrator(
            new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>()),
            Substitute.For<ILogger<EventSequenceMigrator>>());
        _identities = CreateIdentities();
        _eventTypes = CreateEventTypes();
        _storage = OpenStorage();

        await using (var context = CreateContext())
        {
            await context.EnsureTableExists();
        }

        await Append(PlainEvent, FirstGeneration, new() { [FirstGeneration.Generation] = Content("original") }, new() { [FirstGeneration.Generation] = "hash-original" });
        await Append(EventRevisedAtGenerationOne, FirstGeneration, new() { [FirstGeneration.Generation] = Content("original") }, new() { [FirstGeneration.Generation] = "hash-original" });
        await Append(
            EventRevisedAtGenerationTwo,
            SecondGeneration,
            new() { [FirstGeneration.Generation] = Content("original"), [SecondGeneration.Generation] = Content("migrated") },
            new() { [FirstGeneration.Generation] = "hash-original", [SecondGeneration.Generation] = "hash-migrated" });

        await using (var context = CreateContext())
        {
            // The old Revise overwrote the revised generation's content and hash in place and set RevisedGeneration.
            const string revisedAtOne = """{"1":{"value":"legacy-one"}}""";
            const string revisedAtOneHashes = """{"1":"hash-legacy-one"}""";
            const string revisedAtTwo = """{"1":{"value":"original"},"2":{"value":"legacy-two"}}""";
            const string revisedAtTwoHashes = """{"1":"hash-original","2":"hash-legacy-two"}""";
            await context.Database.ExecuteSqlAsync($"UPDATE \"event-sequence\" SET \"Content\" = {revisedAtOne}, \"ContentHashes\" = {revisedAtOneHashes}, \"RevisedGeneration\" = 1 WHERE \"SequenceNumber\" = 1");
            await context.Database.ExecuteSqlAsync($"UPDATE \"event-sequence\" SET \"Content\" = {revisedAtTwo}, \"ContentHashes\" = {revisedAtTwoHashes}, \"RevisedGeneration\" = 2 WHERE \"SequenceNumber\" = 2");
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"event-sequence\" DROP COLUMN \"Revisions\"");
        }

        _migrator.ClearMigrationCache(_connectionString);
    }

    async Task Destroy()
    {
        if (_connectionString is not null)
        {
            await DropDatabase(_connectionString);
        }
    }

    Task Append(ulong sequenceNumber, EventType eventType, Dictionary<EventTypeGeneration, ExpandoObject> content, Dictionary<EventTypeGeneration, EventHash> hashes) =>
        _storage.Append(
            sequenceNumber,
            EventSourceType.Default,
            EventSource,
            EventStreamType.All,
            EventStreamId.Default,
            eventType,
            CorrelationId.New(),
            [],
            [AliceId],
            [],
            DateTimeOffset.UtcNow,
            content,
            hashes);

    IEventSequenceStorage OpenStorage()
    {
        var database = Substitute.For<IDatabase>();
        database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventSequenceDbContext>(CreateContext(), () => { })));
        return new EventSequenceStorage(
            "test-store",
            "test-namespace",
            new EventSequenceId(TableName),
            database,
            _identities,
            Substitute.For<ILogger<EventSequenceStorage>>(),
            _eventTypes,
            new Json.ExpandoObjectConverter(new Schemas.TypeFormats()));
    }

    static IIdentityStorage CreateIdentities()
    {
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Task.FromResult(new Identity(AliceId.Value.ToString(), "alice", "alice")));
        return identities;
    }

    static IEventTypesStorage CreateEventTypes()
    {
        var eventTypes = Substitute.For<IEventTypesStorage>();
        var schema = Schemas.JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string"}}}""").GetAwaiter().GetResult();
        eventTypes.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(true);
        eventTypes.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(call => new EventTypeSchema(new EventType(call.Arg<EventTypeId>(), call.Arg<EventTypeGeneration?>() ?? EventTypeGeneration.First), EventTypeOwner.None, EventTypeSource.Unknown, schema));
        return eventTypes;
    }
}
