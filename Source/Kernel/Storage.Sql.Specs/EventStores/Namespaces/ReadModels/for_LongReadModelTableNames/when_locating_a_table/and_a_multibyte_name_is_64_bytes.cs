// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelTableNames.when_locating_a_table;

public class and_a_multibyte_name_is_64_bytes : Specification
{
    ReadModelTableLocation _result;

    void Because() => _result = LongReadModelTableNames.Locate(new string('x', 60) + "😀", DatabaseType.PostgreSql, string.Empty);

    [Fact] void should_be_qualified() => _result.Schema.ShouldEqual("chronicle_long_names");
    [Fact] void should_fit_the_postgresql_limit() => System.Text.Encoding.UTF8.GetByteCount(_result.Table).ShouldBeLessThanOrEqual(63);
}
