// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_wiping;

public class and_another_sqlite_instance_has_cached_migrations : given.a_sqlite_database
{
    Database _other;
    string[] _pending;

    async Task Establish()
    {
        _other = CreateDatabase();
        await using var scope = await _other.Namespace("store", "namespace");
        _connectionString = scope.DbContext.Database.GetConnectionString()!;
    }

    async Task Because()
    {
        await _database.Wipe();
        await using var scope = await _other.Namespace("store", "namespace");
        _pending = (await scope.DbContext.Database.GetPendingMigrationsAsync()).ToArray();
    }

    [Fact] void should_invalidate_the_other_instances_cache() => _pending.ShouldBeEmpty();
}
#endif
