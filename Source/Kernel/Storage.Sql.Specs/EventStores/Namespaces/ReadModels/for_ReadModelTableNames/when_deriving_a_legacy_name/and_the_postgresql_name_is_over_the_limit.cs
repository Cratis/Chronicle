// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_legacy_name;

public class and_the_postgresql_name_is_over_the_limit : Specification
{
    string _name = new('x', 64);
    string _result;

    void Because() => _result = ReadModelTableNames.Legacy(DatabaseType.PostgreSql, _name);

    [Fact] void should_match_the_stored_truncation() => _result.ShouldEqual(PrimaryKeyNames.TableIdentifier(DatabaseType.PostgreSql, _name));
    [Fact] void should_differ_from_the_candidate() => _result.ShouldNotEqual(ReadModelTableNames.Candidate(DatabaseType.PostgreSql, _name));
    [Fact] void should_identify_a_legacy_name() => ReadModelTableNames.HasLegacyName(DatabaseType.PostgreSql, _name).ShouldBeTrue();
    [Fact] void should_not_identify_a_short_name_as_legacy() => ReadModelTableNames.HasLegacyName(DatabaseType.PostgreSql, "orders").ShouldBeFalse();
}
