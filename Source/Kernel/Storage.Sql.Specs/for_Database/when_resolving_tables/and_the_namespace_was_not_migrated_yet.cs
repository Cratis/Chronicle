// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_tables;

[Collection(PostgreSqlCollection.Name)]
public class and_the_namespace_was_not_migrated_yet(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _tableDatabase;
    string _namespaceDatabase;

    void Establish() => Store = new string('s', 90);

    async Task Because()
    {
        await using var table = await Database.EventSequenceTable(Store, Namespace, "EventLog");
        _tableDatabase = table.DbContext.Database.GetDbConnection().Database;
        await using var ns = await Database.Namespace(Store, Namespace);
        _namespaceDatabase = ns.DbContext.Database.GetDbConnection().Database;
    }

    [Fact] void should_bind_the_same_database_on_both_paths() => _tableDatabase.ShouldEqual(_namespaceDatabase);
}
