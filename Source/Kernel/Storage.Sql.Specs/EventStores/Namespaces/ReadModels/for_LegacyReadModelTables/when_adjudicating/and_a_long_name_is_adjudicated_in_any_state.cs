// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LegacyReadModelTables.when_adjudicating;

public class and_a_long_name_is_adjudicated_in_any_state : Specification
{
    LegacyReadModelRefusal[] _results;

    void Because() => _results =
    [
        .. from canonical in new[] { false, true }
           from legacy in new[] { false, true }
           from key in new[] { false, true }
           from marker in new[] { false, true }
           select LegacyReadModelTables.Adjudicate(new(false, canonical, legacy, key, marker))
    ];

    [Fact] void should_have_covered_every_combination() => _results.Length.ShouldEqual(16);
    [Fact] void should_never_permit_use_adoption_or_creation() => _results.Count(result => result == LegacyReadModelRefusal.None).ShouldEqual(0);
}
