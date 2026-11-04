// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_TableMigrator.when_checking_a_hostile_table_name;

/// <summary>
/// The table name of a column lookup closes the string literal and comments out the rest of the statement.
/// </summary>
public class and_asking_whether_a_column_exists : given.a_sqlite_table_migrator_with_a_canary
{
    bool _exists;
    Exception _error;

    async Task Because()
    {
        await using var context = CreateContext(Canary);
        _error = await Catch.Exception(async () => _exists = await _migrator.ColumnExists(context, "x' OR 1=1 --", "Id"));
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_find_the_column() => _exists.ShouldBeFalse();
}
