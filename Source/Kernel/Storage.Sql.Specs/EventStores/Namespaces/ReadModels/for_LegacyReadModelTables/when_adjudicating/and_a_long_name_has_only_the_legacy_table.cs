// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LegacyReadModelTables.when_adjudicating;

public class and_a_long_name_has_only_the_legacy_table : Specification
{
    LegacyReadModelRefusal _plain;
    LegacyReadModelRefusal _withMatchingKeyAndMarker;

    void Because()
    {
        _plain = LegacyReadModelTables.Adjudicate(new(NameFits: false, CanonicalExists: false, LegacyExists: true));
        _withMatchingKeyAndMarker = LegacyReadModelTables.Adjudicate(new(NameFits: false, CanonicalExists: false, LegacyExists: true, LegacyPrimaryKeyMatches: true, SoleMarkerClaimsLegacy: true));
    }

    // #4340: no ownership authority is established, so there is no automatic adoption and no legacy routing.
    [Fact] void should_refuse_the_adoption() => _plain.ShouldEqual(LegacyReadModelRefusal.LegacyTableRequiresAdoption);
    [Fact] void should_not_treat_a_matching_primary_key_or_a_sole_marker_as_ownership() => _withMatchingKeyAndMarker.ShouldEqual(LegacyReadModelRefusal.LegacyTableRequiresAdoption);
}
