// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LegacyReadModelTables.when_adjudicating;

public class and_a_long_name_has_both_the_canonical_and_the_legacy_table : Specification
{
    LegacyReadModelRefusal _plain;
    LegacyReadModelRefusal _withMatchingKeyAndMarker;

    void Because()
    {
        _plain = LegacyReadModelTables.Adjudicate(new(NameFits: false, CanonicalExists: true, LegacyExists: true));
        _withMatchingKeyAndMarker = LegacyReadModelTables.Adjudicate(new(NameFits: false, CanonicalExists: true, LegacyExists: true, LegacyPrimaryKeyMatches: true, SoleMarkerClaimsLegacy: true));
    }

    [Fact] void should_refuse_the_ambiguity() => _plain.ShouldEqual(LegacyReadModelRefusal.CanonicalAndLegacyTablesExist);
    [Fact] void should_refuse_even_with_a_matching_primary_key_and_a_sole_marker() => _withMatchingKeyAndMarker.ShouldEqual(LegacyReadModelRefusal.CanonicalAndLegacyTablesExist);
}
