// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_candidate;

public class and_the_provider_is_sqlite : Specification
{
    string _name = new('x', 300);
    string _result;

    void Because() => _result = ReadModelTableNames.Candidate(DatabaseType.Sqlite, _name);

    [Fact] void should_keep_the_unlimited_name() => _result.ShouldEqual(_name);
}
