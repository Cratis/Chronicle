// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseName.when_selecting_a_physical_name;

public class and_two_names_share_the_legacy_prefix : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = DatabaseName.Physical(new string('x', 63) + "first", DatabaseType.PostgreSql);
        _second = DatabaseName.Physical(new string('x', 63) + "second", DatabaseType.PostgreSql);
    }

    [Fact] void should_select_distinct_physical_names() => _first.ShouldNotEqual(_second);
}
