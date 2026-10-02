// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_sql_event : a_stored_event
{
    protected EventSequenceStorage _sql;
    SqliteConnection _connection;

    async Task Establish()
    {
        var connectionString = $"DataSource=verify_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(connectionString);
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<EventSequenceDbContext>().UseSqlite(connectionString).AddConceptAsSupport().Options;
        var migrator = new EventSequenceMigrator(new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance), NullLogger<EventSequenceMigrator>.Instance);
        await using (var context = new EventSequenceDbContext(options, "log", migrator))
        {
            await context.EnsureTableExists();
        }

        var database = Substitute.For<IDatabase>();
        database.EventSequenceTable("store", "tenant", "log").Returns(_ => new DbContextScope<EventSequenceDbContext>(new EventSequenceDbContext(options, "log", migrator), () => { }));
        _sql = new("store", "tenant", "log", database, Substitute.For<IIdentityStorage>(), NullLogger<EventSequenceStorage>.Instance);
        _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log").Returns(_sql);
        dynamic first = new ExpandoObject();
        first.value = 42;
        dynamic second = new ExpandoObject();
        second.renamed = 42;
        var appended = await _sql.Append(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", 1), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new Dictionary<EventTypeGeneration, ExpandoObject> { [1] = first, [2] = second }, new Dictionary<EventTypeGeneration, EventHash>());
        appended.IsSuccess.ShouldBeTrue();
    }

    protected async Task Revise()
    {
        dynamic content = new ExpandoObject();
        content.value = 43;
        await _sql.Revise(EventSequenceNumber.First, new("event", 1), CorrelationId.New(), [], [], DateTimeOffset.UtcNow, (ExpandoObject)content, EventHash.NotSet);
        using var cursor = await _sql.GetRange(EventSequenceNumber.First, EventSequenceNumber.First);
        (await cursor.MoveNext()).ShouldBeTrue();
        _stored = cursor.Current.Single();
        _stored.GenerationalContent[1].ShouldContain("43");
        _stored.GenerationalContent[2].ShouldContain("42");
    }

    async Task Destroy() => await _connection.DisposeAsync();
}
