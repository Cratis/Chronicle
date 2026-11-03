// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelTableNames.when_locating_a_table;

public class and_two_backups_of_a_60_byte_container_are_taken : Specification
{
    const string Container = "container_name_that_is_exactly_sixty_bytes_long_for_the_spec";
    ReadModelTableLocation _live;
    ReadModelTableLocation _first;
    ReadModelTableLocation _second;

    void Because()
    {
        _live = LongReadModelTableNames.Locate(Container, DatabaseType.PostgreSql, string.Empty);
        _first = LongReadModelTableNames.Locate(Container + "_backup_a", DatabaseType.PostgreSql, string.Empty);
        _second = LongReadModelTableNames.Locate(Container + "_backup_b", DatabaseType.PostgreSql, string.Empty);
    }

    [Fact] void should_have_a_60_byte_container() => Container.Length.ShouldEqual(60);
    [Fact] void should_keep_the_live_table_unqualified() => _live.Schema.ShouldBeNull();
    [Fact] void should_give_the_backups_distinct_tables() => _first.Table.ShouldNotEqual(_second.Table);
    [Fact] void should_not_reuse_the_live_table_name() => new[] { _first.Table, _second.Table }.ShouldNotContain(_live.Table);
    [Fact] void should_place_both_backups_in_the_long_name_schema() => new[] { _first.Schema, _second.Schema }.Count(schema => schema == "chronicle_long_names").ShouldEqual(2);
}
