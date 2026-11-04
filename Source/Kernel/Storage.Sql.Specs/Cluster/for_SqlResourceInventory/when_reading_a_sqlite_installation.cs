// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceInventory;

public class when_reading_a_sqlite_installation : for_Database.given.a_sqlite_database
{
    ResourceUse[] _uses;
    int _bindings;

    async Task Establish()
    {
        await using var scope = await _database.Cluster();
        scope.DbContext.EventStores.Add(new EventStore { Name = "orders" });
        await scope.DbContext.SaveChangesAsync();
    }

    async Task Because()
    {
        _uses = [.. await new SqlResourceInventory(_database).Read(CancellationToken.None)];
        await using var scope = await _database.Cluster();
        _bindings = scope.DbContext.DatabaseNames.Count();
    }

    [Fact] void should_report_the_cluster_for_the_kernel() => _uses.Count(_ => _.Owner is null).ShouldEqual(1);
    [Fact] void should_report_the_registered_store() => _uses.Any(_ => _.Owner?.EventStore == "orders" && _.Owner.Namespace is null).ShouldBeTrue();
    [Fact] void should_report_the_system_store() => _uses.Any(_ => _.Owner?.EventStore == "System" && _.Owner.Namespace is null).ShouldBeTrue();
    [Fact] void should_keep_the_databases_apart() => _uses.Select(_ => _.Identity).Distinct().Count().ShouldEqual(_uses.Length);
    [Fact] void should_not_write_a_binding() => _bindings.ShouldEqual(0);
}
