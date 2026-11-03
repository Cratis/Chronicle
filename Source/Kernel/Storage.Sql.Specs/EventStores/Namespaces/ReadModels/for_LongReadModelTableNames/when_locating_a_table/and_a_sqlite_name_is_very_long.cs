// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelTableNames.when_locating_a_table;

public class and_a_sqlite_name_is_very_long : Specification
{
    readonly string _name = new('x', 300);
    ReadModelTableLocation _result;

    void Because() => _result = LongReadModelTableNames.Locate(_name, DatabaseType.Sqlite, string.Empty);

    [Fact] void should_not_be_qualified() => _result.Schema.ShouldBeNull();
    [Fact] void should_keep_the_identical_name_instance() => ReferenceEquals(_result.Table, _name).ShouldBeTrue();
}
