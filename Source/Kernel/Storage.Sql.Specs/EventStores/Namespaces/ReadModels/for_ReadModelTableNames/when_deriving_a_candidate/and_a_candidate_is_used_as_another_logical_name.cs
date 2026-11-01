// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_candidate;

public class and_a_candidate_is_used_as_another_logical_name : Specification
{
    string _longName = new('x', 64);
    string _shortName;
    string _longCandidate;
    string _shortCandidate;

    void Establish() => _shortName = new string('x', 46) + "~7ce100971f64e700";

    void Because()
    {
        _longCandidate = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, _longName);
        _shortCandidate = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, _shortName);
    }

    [Fact] void should_have_distinct_logical_names() => _longName.ShouldNotEqual(_shortName);
    [Fact] void should_demonstrate_that_candidates_do_not_prove_ownership() => _longCandidate.ShouldEqual(_shortCandidate);
}
