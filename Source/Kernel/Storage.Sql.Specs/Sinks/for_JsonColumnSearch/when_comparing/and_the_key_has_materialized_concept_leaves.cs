// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_JsonColumnSearch.when_comparing;

public class and_the_key_has_materialized_concept_leaves : given.materialized_children
{
    bool[] _matches;

    void Because() => _matches = _children.Cast<IDictionary<string, object?>>().Select(child => JsonColumnSearch.CompareLoose(child["id"], _key)).ToArray();

    [Fact] void should_match_only_the_full_unambiguous_identity() => _matches.ShouldEqual(false, false, true, false);
}
