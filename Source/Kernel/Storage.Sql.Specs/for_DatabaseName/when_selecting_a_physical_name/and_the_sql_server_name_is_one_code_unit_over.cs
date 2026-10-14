// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseName.when_selecting_a_physical_name;

public class and_the_sql_server_name_is_one_code_unit_over : Specification
{
    string _result;

    void Because() => _result = DatabaseName.Physical(new string('x', 129), DatabaseType.SqlServer);

    [Fact] void should_fit_with_a_hash() => _result.Length.ShouldEqual(128);
    [Fact] void should_keep_the_readable_prefix() => _result[..112].ShouldEqual(new string('x', 111) + "~");
}
