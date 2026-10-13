// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage;

public class when_clearing_default_mode_claims : Specification
{
    UniqueConstraintsStorage _storage;
    UniqueConstraintDefinition _definition;
    SqliteConnection _connection;

    async Task Establish()
    {
        _connection = new($"DataSource=clear-claims-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        var migrator = new UniqueConstraintMigrator(
            new TableMigrator<UniqueConstraintDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintDbContext>>>()),
            new TableMigrator<UniqueConstraintValuesDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintValuesDbContext>>>()),
            Substitute.For<ILogger<UniqueConstraintMigrator>>());
        var database = Substitute.For<IDatabase>();
        database.UniqueConstraintTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>()).Returns(async call =>
        {
            var context = new UniqueConstraintDbContext(
                new DbContextOptionsBuilder<UniqueConstraintDbContext>().UseSqlite(_connection.ConnectionString).AddConceptAsSupport().Options,
                call.ArgAt<string>(2),
                migrator);
            await context.EnsureTableExists();
            return new DbContextScope<UniqueConstraintDbContext>(context, () => { });
        });
        database.UniqueConstraintValuesTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>()).Returns(async call =>
        {
            var context = new UniqueConstraintValuesDbContext(
                new DbContextOptionsBuilder<UniqueConstraintValuesDbContext>().UseSqlite(_connection.ConnectionString).AddConceptAsSupport().Options,
                call.ArgAt<string>(2),
                migrator);
            await context.EnsureTableExists();
            return new DbContextScope<UniqueConstraintValuesDbContext>(context, () => { });
        });
        _storage = new("store", "namespace", EventSequenceId.Log, database);
        _definition = new("unique", []);
        await _storage.Save("first", _definition, 1UL, "first-value", "selected");
        await _storage.Save("second", _definition, 2UL, "second-value", "selected");
        await _storage.Save("first", _definition, 3UL, "other-scope", "other");
        await _storage.Save("first", _definition with { Name = "other-constraint" }, 4UL, "other-constraint", "selected");
    }

    async Task Because() => await _storage.ClearValues(_definition, "selected");

    void Destroy() => _connection.Dispose();

    [Fact] async Task should_release_the_first_owners_claim() => (await _storage.IsAllowed("other", _definition, "first-value", "selected")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_release_the_second_owners_claim() => (await _storage.IsAllowed("other", _definition, "second-value", "selected")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_preserve_another_scope() => (await _storage.IsAllowed("other", _definition, "other-scope", "other")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_preserve_another_constraint() => (await _storage.IsAllowed("other", _definition with { Name = "other-constraint" }, "other-constraint", "selected")).IsAllowed.ShouldBeFalse();
}
