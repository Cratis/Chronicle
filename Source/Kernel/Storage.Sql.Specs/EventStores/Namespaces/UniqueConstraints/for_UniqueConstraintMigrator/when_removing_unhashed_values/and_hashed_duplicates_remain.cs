// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_removing_unhashed_values;

public class and_hashed_duplicates_remain : given.a_legacy_sqlite_table
{
    long _deleted;

    async Task Establish()
    {
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO legacy_claims VALUES ('first', {0}, 1), ('second', {0}, 2)", new string('a', 64));
        await _context.EnsureTableExists();
    }

    async Task Because() => _deleted = await UnhashedUniqueConstraintValues.RemoveFrom(_context, TableName, CancellationToken.None);

    [Fact] void should_not_delete_hash_shaped_claims() => _deleted.ShouldEqual(0L);
    [Fact] async Task should_keep_both_owners() => (await _context.Entries.CountAsync()).ShouldEqual(2);
    [Fact] async Task should_keep_the_existing_non_unique_fallback() => (await IndexIsUnique()).ShouldBeFalse();
}
