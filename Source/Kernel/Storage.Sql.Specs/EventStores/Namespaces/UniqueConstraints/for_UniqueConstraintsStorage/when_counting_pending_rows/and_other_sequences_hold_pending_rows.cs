// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_counting_pending_rows;

[Collection(SqlServerCollection.Name)]
public class and_other_sequences_hold_pending_rows(SqlServerFixture fixture) : given.a_server_claims_storage(fixture)
{
    int _before;
    int _after;

    async Task Establish()
    {
        await Prepare(false);
        await CreateLegacySourceTable(_sourceTable);
        await CreateLegacyValuesTable(_valuesTable);
        await CreateLegacySourceTable("other_sequence_unique_constraint");
        await AddLegacyClaim(_sourceTable, "Alice", "one");
        await AddLegacyClaim(_sourceTable, "Bob", "two");
        await AddLegacyClaim(_valuesTable, "one", "Alice");
        await AddLegacyClaim("other_sequence_unique_constraint", "Alice", "one");
        await EnsureTables();
        await using var other = new UniqueConstraintDbContext(new DbContextOptionsBuilder<UniqueConstraintDbContext>().UseSqlServer(_connectionString).Options, "other_sequence_unique_constraint", _migrator);
        await other.EnsureTableExists();
    }

    async Task Because()
    {
        _before = await _storage.CountPendingSpellings();
        await _storage.ClearValues(_perSource);
        await _storage.ClearValues(_perValue);
        await _storage.Save("Alice", _perSource, 1UL, "one");
        _after = await _storage.CountPendingSpellings();
    }

    [Fact] void should_count_the_pending_rows_of_both_table_shapes_and_no_other_sequence() => _before.ShouldEqual(3);
    [Fact] void should_count_nothing_once_the_rows_are_rebuilt() => _after.ShouldEqual(0);
}
