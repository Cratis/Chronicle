// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseName.when_selecting_a_physical_name;

public class and_the_postgresql_name_is_one_byte_over : Specification
{
    string _result;

    void Because() => _result = DatabaseName.Physical(new string('x', 64), DatabaseType.PostgreSql);

    [Fact] void should_fit_with_a_stable_hash() => _result.ShouldEqual(new string('x', 46) + "~7ce100971f64e700");
}
