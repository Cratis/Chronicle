// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseName.when_selecting_a_physical_name;

public class and_the_postgresql_name_is_at_the_limit : Specification
{
    readonly string _name = new('x', 63);
    string _result;

    void Because() => _result = DatabaseName.Physical(_name, DatabaseType.PostgreSql);

    [Fact] void should_leave_the_name_unchanged() => _result.ShouldEqual(_name);
}
