// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

/// <summary>
/// Sets up a <see cref="UniqueConstraintsStorage"/> backed by a shared in-memory SQLite database.
/// A single open connection is shared across every <see cref="IDatabase.UniqueConstraintTable"/> scope so saved
/// claims survive between the storage's individual unit-of-work scopes.
/// </summary>
public class a_unique_constraints_storage : Specification
{
    protected const string ConstraintNameValue = "unique-name";
    protected static readonly EventStoreName _eventStore = "test-store";
    protected static readonly EventStoreNamespaceName _namespace = "test-namespace";
    protected SqliteConnection _connection;
    protected UniqueConstraintDefinition _definition;
    protected UniqueConstraintsStorage _storage;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var schemaContext = CreateContext())
        {
            schemaContext.Database.EnsureCreated();
        }

        var database = Substitute.For<IDatabase>();
        database.UniqueConstraintTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<UniqueConstraintDbContext>(CreateContext(), () => { })));

        _definition = new(ConstraintNameValue, []);
        _storage = new UniqueConstraintsStorage(_eventStore, _namespace, EventSequenceId.Log, database);
    }

    void Destroy() => _connection.Dispose();

    UniqueConstraintDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UniqueConstraintDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new UniqueConstraintDbContext(options, $"{EventSequenceId.Log}_{ConstraintNameValue}_constraint", Substitute.For<IUniqueConstraintMigrator>());
    }
}
