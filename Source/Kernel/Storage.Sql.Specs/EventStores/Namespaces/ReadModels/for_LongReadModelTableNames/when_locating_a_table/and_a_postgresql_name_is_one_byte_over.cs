// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelTableNames.when_locating_a_table;

public class and_a_postgresql_name_is_one_byte_over : Specification
{
    ReadModelTableLocation _result;

    void Because() => _result = LongReadModelTableNames.Locate(new('x', 64), DatabaseType.PostgreSql, string.Empty);

    [Fact] void should_use_the_long_name_schema() => _result.Schema.ShouldEqual("chronicle_long_names");
    [Fact] void should_use_a_readable_prefix_and_a_128_bit_hash() => _result.Table.ShouldEqual(new string('x', 30) + "_7ce100971f64e7001e8fe5a51973ecdf");
    [Fact] void should_fit_the_postgresql_limit() => System.Text.Encoding.UTF8.GetByteCount(_result.Table).ShouldEqual(63);
}
