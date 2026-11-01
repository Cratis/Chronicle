// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_candidate;

public class and_two_backups_differ_only_after_the_postgresql_limit : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, new string('x', 60) + "-20260607123456-11111111");
        _second = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, new string('x', 60) + "-20260607123456-22222222");
    }

    [Fact] void should_have_distinct_names() => _first.ShouldNotEqual(_second);
    [Fact] void should_fit_the_first_name() => (Encoding.UTF8.GetByteCount(_first) <= 63).ShouldBeTrue();
    [Fact] void should_fit_the_second_name() => (Encoding.UTF8.GetByteCount(_second) <= 63).ShouldBeTrue();
}
