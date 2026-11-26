// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.given;

/// <summary>
/// Compares the actual storage reads before and after removing a sequence index on an isolated table.
/// </summary>
public static class SequenceIndexReadCorpus
{
    static readonly EventType _type = new("selected", EventTypeGeneration.First);

    /// <summary>
    /// Creates the identities expected by the storage materializer.
    /// </summary>
    /// <returns>The identity storage.</returns>
    public static IIdentityStorage Identities()
    {
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<Cratis.Chronicle.Concepts.Identities.IdentityId>>()).Returns(Cratis.Chronicle.Concepts.Identities.Identity.System);
        return identities;
    }

    /// <summary>
    /// Seeds sparse events, verifies reads, removes the index, restarts, and repeats reads and duplicate detection.
    /// </summary>
    /// <param name="host">An isolated provider host.</param>
    /// <returns>The verification task.</returns>
    public static async Task Verify(EventSequenceStorageHost host)
    {
        var storage = host.Open();
        var events = Enumerable.Range(0, 120).Select(index => Event((ulong)index * 3)).ToArray();
        (await storage.AppendMany(events)).IsSuccess.ShouldBeTrue();
        await using (var context = host.CreateContext())
        {
            var table = context.Model.FindEntityType(typeof(EventEntry))!.GetTableName()!;
            var index = $"IX_{table}_SequenceNumber";
#pragma warning disable EF1002 // Fixture-generated identifiers only.
            if (context.Database.IsNpgsql())
            {
                // Reconstruct the pre-change shape, then exercise the normal upgrade/cache lifecycle.
                await context.Database.ExecuteSqlRawAsync($"CREATE INDEX \"{index}\" ON \"{table}\" (\"SequenceNumber\")");
            }
        }
        var before = await Read(storage);
        await using (var context = host.CreateContext())
        {
            var table = context.Model.FindEntityType(typeof(EventEntry))!.GetTableName()!;
            var index = $"IX_{table}_SequenceNumber";
            if (context.Database.IsSqlite())
            {
                // Experiment only: SQLite production creation and upgrade deliberately retain this index.
                await context.Database.ExecuteSqlRawAsync($"DROP INDEX \"{index}\"");
            }
#pragma warning restore EF1002
            new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>())
                .ClearMigrationCacheForConnectionString(context.Database.GetConnectionString()!);
            await context.EnsureTableExists();
            var sql = context.Database.IsNpgsql()
                ? "SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname = current_schema() AND indexname = {0}"
                : "SELECT name AS Value FROM sqlite_master WHERE type = 'index' AND name = {0}";
            var remaining = await context.Database.SqlQueryRaw<string>(sql, index).ToArrayAsync();
            remaining.ShouldBeEmpty();
        }
        Assert.Equal(before, await Read(host.Open()));
        var duplicate = await host.Open().AppendMany([events[0]]);
        duplicate.IsSuccess.ShouldBeFalse();
        duplicate.AsT1.NextAvailableSequenceNumber.Value.ShouldEqual(358UL);
        (await host.Open().GetCount()).Value.ShouldEqual(120UL);
    }

    static EventToAppendToStorage Event(ulong number) => new(
        number,
        EventSourceType.Default,
        number % 2 == 0 ? "selected-source" : "other-source",
        EventStreamType.All,
        EventStreamId.Default,
        number % 2 == 0 ? _type : new EventType("other", EventTypeGeneration.First),
        CorrelationId.New(),
        [],
        [],
        [],
        DateTimeOffset.UtcNow,
        new ExpandoObject(),
        EventHash.NotSet);

    static async Task<string[]> Read(IEventSequenceStorage storage)
    {
        var results = new List<string>
        {
            string.Join(',', await Numbers(await storage.GetFromSequenceNumber(0))),
            string.Join(',', await Numbers(await storage.GetFromSequenceNumber(181))),
            string.Join(',', await Numbers(await storage.GetRange(30, 90, eventTypes: [_type]))),
            string.Join(',', await Numbers(await storage.GetRange(30, 90, eventSourceId: "selected-source"))),
            string.Join(',', await Numbers(await storage.GetRange(30, 90, eventSourceId: "selected-source", eventTypes: [_type]))),
            (await storage.GetHeadSequenceNumber()).Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            (await storage.GetTailSequenceNumber()).Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            (await storage.GetNextSequenceNumberGreaterOrEqualThan(181)).Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            string.Join(',', (await storage.GetMetadataAt([0, 180, 357])).OrderBy(entry => entry.SequenceNumber.Value).Select(entry => entry.SequenceNumber.Value)),
            (await storage.GetCount()).Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        var parent = await storage.GetInstancesOfAnyInRange(["selected-source"], [_type.Id], 181, 200);
        results.Add(string.Join(',', parent.Values.SelectMany(events => events).Select(entry => entry.Context.SequenceNumber.Value)));
        results[0].ShouldEqual(string.Join(',', Enumerable.Range(0, 120).Select(index => index * 3)));
        results[1].ShouldEqual(string.Join(',', Enumerable.Range(61, 59).Select(index => index * 3)));
        results[2].ShouldEqual("30,36,42,48,54,60,66,72,78,84,90");
        results[3].ShouldEqual(results[2]);
        results[4].ShouldEqual(results[2]);
        results[5].ShouldEqual("0");
        results[6].ShouldEqual("357");
        results[7].ShouldEqual("183");
        results[8].ShouldEqual("0,180,357");
        results[9].ShouldEqual("120");
        results[10].ShouldEqual("180,186,192,198");
        var tails = await storage.GetTailSequenceNumbersForEventTypes([_type, new("other", EventTypeGeneration.First)]);
        tails[_type].Value.ShouldEqual(354UL);
        tails[new("other", EventTypeGeneration.First)].Value.ShouldEqual(357UL);
        results.Add(string.Join(',', tails.OrderBy(entry => entry.Key.Id.Value).Select(entry => entry.Value.Value)));
        return results.ToArray();
    }

    static async Task<ulong[]> Numbers(IEventCursor cursor)
    {
        using (cursor)
        {
            var numbers = new List<ulong>();
            while (await cursor.MoveNext()) numbers.AddRange(cursor.Current.Select(entry => entry.Context.SequenceNumber.Value));
            return numbers.ToArray();
        }
    }
}
