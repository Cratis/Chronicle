// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelTableNames.when_locating_a_table;

public class and_the_same_name_is_located_twice : Specification
{
    ReadModelTableLocation _first;
    ReadModelTableLocation _second;

    void Because()
    {
        _first = LongReadModelTableNames.Locate(new('x', 90), DatabaseType.PostgreSql, "tenant");
        _second = LongReadModelTableNames.Locate(new('x', 90), DatabaseType.PostgreSql, "tenant");
    }

    [Fact] void should_produce_the_same_location() => _second.ShouldEqual(_first);
}
