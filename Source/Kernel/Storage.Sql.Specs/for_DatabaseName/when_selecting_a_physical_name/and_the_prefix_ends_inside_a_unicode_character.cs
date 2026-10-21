// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseName.when_selecting_a_physical_name;

public class and_the_prefix_ends_inside_a_unicode_character : Specification
{
    string _postgres;
    string _sqlServer;

    void Because()
    {
        _postgres = DatabaseName.Physical(new string('x', 45) + "😀" + new string('y', 40), DatabaseType.PostgreSql);
        _sqlServer = DatabaseName.Physical(new string('x', 110) + "😀" + new string('y', 40), DatabaseType.SqlServer);
    }

    [Fact] void should_not_split_utf8() => _postgres[..46].ShouldEqual(new string('x', 45) + "~");
    [Fact] void should_not_split_a_surrogate_pair() => _sqlServer[..111].ShouldEqual(new string('x', 110) + "~");
}
