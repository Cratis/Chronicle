// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents.for_InFlightEventsStorage;

[Collection(SqlServerCollection.Name)]
public class when_partitions_differ_by_spelling_on_sql_server(SqlServerFixture fixture) : Specification
{
    static readonly EventStoreName _eventStore = "spelling-store";
    static readonly EventStoreNamespaceName _namespace = "spelling-namespace";
    string _connectionString;
    ulong[] _remaining;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    async Task Because()
    {
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), () => { })));
        var storage = new InFlightEventsStorage(_eventStore, _namespace, database);

        await storage.Add("observer", "Partition", 1UL);
        await storage.Add("observer", "partition", 2UL);
        await storage.Add("observer", "Partition ", 3UL);
        await storage.Add("observer", "\uFF30artition", 4UL);

        await storage.Remove("observer", "partition", 1UL);
        await storage.RemoveUpTo("observer", "Partition ", 4UL);
        _remaining = (await storage.GetFor("observer")).Select(_ => _.EventSequenceNumber.Value).Order().ToArray();
    }

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    [Fact] void should_remove_only_the_entries_of_the_exact_partition() => _remaining.ShouldEqual([1UL, 2UL, 4UL]);

    NamespaceDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<NamespaceDbContext>().AddConceptAsSupport().WithoutServiceProviderCaching().UseSqlServer(_connectionString).Options);
}
