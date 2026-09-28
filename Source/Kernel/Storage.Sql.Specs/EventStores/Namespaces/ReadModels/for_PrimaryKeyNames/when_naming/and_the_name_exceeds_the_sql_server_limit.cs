// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_PrimaryKeyNames.when_naming;

public class and_the_name_exceeds_the_sql_server_limit : Specification
{
    static readonly string _container = new('x', 113);

    string _first;
    string _second;

    void Because()
    {
        _first = PrimaryKeyNames.For(DatabaseType.SqlServer, $"{_container}-20260101120000");
        _second = PrimaryKeyNames.For(DatabaseType.SqlServer, $"{_container}-20260101120100");
    }

    [Fact] void should_give_them_distinct_names() => _first.ShouldNotEqual(_second);
    [Fact] void should_keep_the_first_within_the_limit() => _first.Length.ShouldEqual(PrimaryKeyNames.SqlServerMaxIdentifierLength);
    [Fact] void should_keep_the_second_within_the_limit() => _second.Length.ShouldEqual(PrimaryKeyNames.SqlServerMaxIdentifierLength);
}
