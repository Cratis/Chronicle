// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelTableNames.when_locating_a_table;

public class and_the_clip_ends_inside_a_multibyte_character : Specification
{
    ReadModelTableLocation _postgres;
    ReadModelTableLocation _sqlServer;

    void Because()
    {
        _postgres = LongReadModelTableNames.Locate(new string('x', 29) + "😀" + new string('y', 40), DatabaseType.PostgreSql, string.Empty);
        _sqlServer = LongReadModelTableNames.Locate(new string('x', 94) + "😀" + new string('y', 40), DatabaseType.SqlServer, string.Empty);
    }

    [Fact] void should_not_split_utf8() => _postgres.Table[..30].ShouldEqual(new string('x', 29) + "_");
    [Fact] void should_not_split_a_surrogate_pair() => _sqlServer.Table[..95].ShouldEqual(new string('x', 94) + "_");
    [Fact] void should_produce_valid_utf8_within_the_postgresql_limit() => System.Text.Encoding.UTF8.GetByteCount(_postgres.Table).ShouldBeLessThanOrEqual(63);
    [Fact] void should_stay_within_the_sql_server_limit() => _sqlServer.Table.Length.ShouldBeLessThanOrEqual(128);
}
