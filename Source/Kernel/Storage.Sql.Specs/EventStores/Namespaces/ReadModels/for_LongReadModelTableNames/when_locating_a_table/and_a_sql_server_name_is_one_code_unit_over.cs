// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelTableNames.when_locating_a_table;

public class and_a_sql_server_name_is_one_code_unit_over : Specification
{
    ReadModelTableLocation _result;

    void Because() => _result = LongReadModelTableNames.Locate(new('x', 129), DatabaseType.SqlServer, string.Empty);

    [Fact] void should_use_the_long_name_schema() => _result.Schema.ShouldEqual("chronicle_long_names");
    [Fact] void should_use_a_readable_prefix_and_a_128_bit_hash() => _result.Table.ShouldEqual(new string('x', 95) + "_0ec9eb33e74510bcdd1f2ea55206e82f");
    [Fact] void should_fit_the_sql_server_limit() => _result.Table.Length.ShouldEqual(128);
}
