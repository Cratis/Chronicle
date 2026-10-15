// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_an_event_store;

[Collection(SqlServerCollection.Name)]
public class and_the_sql_server_name_is_over_the_limit(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    string _name;
    int _namespaces;

    void Establish() => Store = new string('s', 150);

    async Task Because()
    {
        await using var scope = await Database.EventStore(Store);
        _name = scope.DbContext.Database.GetDbConnection().Database;
        _namespaces = await scope.DbContext.Namespaces.CountAsync();
    }

    [Fact] void should_create_a_length_safe_database() => _name.Length.ShouldEqual(128);
    [Fact] void should_keep_a_hash_suffix() => _name[111].ShouldEqual('~');
    [Fact] void should_apply_the_event_store_schema() => _namespaces.ShouldEqual(0);
}
