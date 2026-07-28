// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.given;

/// <summary>
/// Sets up an <see cref="EventTypesStorage"/> backed by a shared in-memory SQLite database with an
/// event type that has two generations. The storage is never populated, so its cache stays cold and
/// resolving a schema for a generation falls back to the database.
/// </summary>
public class an_event_types_storage : Specification, IDisposable
{
    protected static readonly EventTypeId _eventTypeId = new("event-type-with-two-generations");
    protected static readonly EventTypeGeneration _firstGeneration = EventTypeGeneration.First;
    protected static readonly EventTypeGeneration _secondGeneration = new(2);

    protected const string FirstGenerationProperty = "firstGenerationValue";
    protected const string SecondGenerationProperty = "secondGenerationValue";

    protected SqliteConnection _connection;
    protected IDatabase _database;
    protected EventTypesStorage _storage;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var schemaContext = CreateContext())
        {
            schemaContext.Database.EnsureCreated();
            schemaContext.EventTypes.Add(new EventType
            {
                Id = _eventTypeId,
                Schemas = new Dictionary<uint, string>
                {
                    { _firstGeneration, SchemaFor(FirstGenerationProperty) },
                    { _secondGeneration, SchemaFor(SecondGenerationProperty) }
                }
            });
            schemaContext.SaveChanges();
        }

        _database = Substitute.For<IDatabase>();
        _database.EventStore(Arg.Any<EventStoreName>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventStoreDbContext>(CreateContext(), () => { })));

        // Deliberately not populated — the cache stays cold so GetFor hits the database fallback path.
        _storage = new EventTypesStorage(EventStoreName.NotSet, _database);
    }

    protected static string SchemaFor(string propertyName) =>
        $$"""
        {
            "type": "object",
            "properties": {
                "{{propertyName}}": { "type": "string" }
            }
        }
        """;

    protected EventStoreDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new EventStoreDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
