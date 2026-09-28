// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_PrimaryKeyNames.when_naming;

public class and_a_multi_byte_character_straddles_the_postgresql_limit : Specification
{
    string _name;

    void Because() => _name = PrimaryKeyNames.For(DatabaseType.PostgreSql, $"{new string('x', 26)}{new string('\u00e6', 20)}");

    [Fact] void should_keep_the_name_within_the_limit() => Encoding.UTF8.GetByteCount(_name).ShouldBeLessThanOrEqual(PrimaryKeyNames.PostgreSqlMaxIdentifierBytes);
}
