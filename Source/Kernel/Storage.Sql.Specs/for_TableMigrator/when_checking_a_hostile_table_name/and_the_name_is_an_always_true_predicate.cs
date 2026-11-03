// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_TableMigrator.when_checking_a_hostile_table_name;

/// <summary>
/// The table name turns the existence check into an always true predicate matching another table, so the table it names is never created.
/// </summary>
public class and_the_name_is_an_always_true_predicate : given.a_sqlite_table_migrator_with_a_canary
{
    async Task Because()
    {
        await using var context = CreateContext("x' OR name='canary");
        await _migrator.EnsureTableMigrated("x' OR name='canary", context, RecordCreation);
    }

    [Fact] void should_create_the_table_it_names() => _creations.ShouldEqual(1);
}
