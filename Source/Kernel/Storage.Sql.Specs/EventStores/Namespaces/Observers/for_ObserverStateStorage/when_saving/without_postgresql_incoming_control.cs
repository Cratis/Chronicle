// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_saving;

[Collection(PostgreSqlCollection.Name)]
public class without_postgresql_incoming_control(PostgreSqlFixture fixture) : Specification
{
    string _connectionString;
    ObserverStateStorage _storage;
    ObserverState _saved;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        context.Observers.Add(new() { Id = "observer", TypeSetControl = "{}" });
        await context.SaveChangesAsync();
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), static () => { })));
        _storage = new(EventStoreName.System, EventStoreNamespaceName.Default, database);
    }

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(123) });
        await using var context = CreateContext();
        _saved = await context.Observers.SingleAsync();
    }

    [Fact] void should_preserve_control_without_parsing_its_shape() => _saved.TypeSetControl.ShouldEqual("{}");
    [Fact] void should_save_progress() => _saved.NextEventSequenceNumber.ShouldEqual(123UL);

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    NamespaceDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<NamespaceDbContext>();
        builder.UseDatabaseFromConnectionString(_connectionString).AddConceptAsSupport().EnableServiceProviderCaching(false);
        return new(builder.Options);
    }
}
