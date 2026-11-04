// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_TableMigrator.when_checking_a_hostile_table_name;

/// <summary>
/// The column name of a column lookup turns the predicate into an always true one, so a column the table lacks seems to exist.
/// </summary>
public class and_the_column_name_is_an_always_true_predicate : given.a_sqlite_table_migrator_with_a_canary
{
    bool _exists;

    async Task Because()
    {
        await using var context = CreateContext(Canary);
        _exists = await _migrator.ColumnExists(context, Canary, "nope' OR '1'='1");
    }

    [Fact] void should_not_find_the_column() => _exists.ShouldBeFalse();
}
