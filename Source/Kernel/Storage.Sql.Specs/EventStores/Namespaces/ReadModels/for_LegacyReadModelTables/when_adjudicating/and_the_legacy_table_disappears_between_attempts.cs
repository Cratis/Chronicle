// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LegacyReadModelTables.when_adjudicating;

public class and_the_legacy_table_disappears_between_attempts : Specification
{
    LegacyReadModelRefusal _before;
    LegacyReadModelRefusal _afterWithCanonical;
    LegacyReadModelRefusal _afterWithNothing;

    void Because()
    {
        _before = LegacyReadModelTables.Adjudicate(new(NameFits: false, CanonicalExists: true, LegacyExists: true));
        _afterWithCanonical = LegacyReadModelTables.Adjudicate(new(NameFits: false, CanonicalExists: true, LegacyExists: false));
        _afterWithNothing = LegacyReadModelTables.Adjudicate(new(NameFits: false, CanonicalExists: false, LegacyExists: false));
    }

    [Fact] void should_have_refused_before() => _before.ShouldNotEqual(LegacyReadModelRefusal.None);
    [Fact] void should_still_refuse_with_the_canonical_table() => _afterWithCanonical.ShouldNotEqual(LegacyReadModelRefusal.None);
    [Fact] void should_still_refuse_with_no_table() => _afterWithNothing.ShouldNotEqual(LegacyReadModelRefusal.None);
}
