// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_candidate;

public class and_the_same_name_is_derived_twice : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, new string('x', 64));
        _second = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, new string('x', 64));
    }

    [Fact] void should_be_deterministic() => _second.ShouldEqual(_first);
    [Fact] void should_keep_the_stable_digest() => _first.ShouldEqual(new string('x', 46) + "~7ce100971f64e700");
}
