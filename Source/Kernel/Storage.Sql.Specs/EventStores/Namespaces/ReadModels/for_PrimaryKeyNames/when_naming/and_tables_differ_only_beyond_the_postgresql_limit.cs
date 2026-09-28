// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_PrimaryKeyNames.when_naming;

public class and_tables_differ_only_beyond_the_postgresql_limit : Specification
{
    const string Container = "read_models_with_a_name_of_forty_eight_character";

    string _first;
    string _second;

    void Because()
    {
        _first = PrimaryKeyNames.For(DatabaseType.PostgreSql, $"{Container}-20260101120000");
        _second = PrimaryKeyNames.For(DatabaseType.PostgreSql, $"{Container}-20260101120100");
    }

    [Fact] void should_give_them_distinct_names() => _first.ShouldNotEqual(_second);
    [Fact] void should_keep_the_first_within_the_limit() => Encoding.UTF8.GetByteCount(_first).ShouldEqual(PrimaryKeyNames.PostgreSqlMaxIdentifierBytes);
    [Fact] void should_keep_the_second_within_the_limit() => Encoding.UTF8.GetByteCount(_second).ShouldEqual(PrimaryKeyNames.PostgreSqlMaxIdentifierBytes);
    [Fact] void should_keep_a_readable_prefix() => _first.StartsWith($"PK_{Container[..40]}", StringComparison.Ordinal).ShouldBeTrue();
}
