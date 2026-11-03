// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LegacyReadModelTables.when_adjudicating;

public class and_the_name_fits : Specification
{
    LegacyReadModelRefusal _bothPresent;
    LegacyReadModelRefusal _neither;
    LegacyReadModelRefusal _legacyOnly;

    void Because()
    {
        _bothPresent = LegacyReadModelTables.Adjudicate(new(NameFits: true, CanonicalExists: true, LegacyExists: true));
        _neither = LegacyReadModelTables.Adjudicate(new(NameFits: true, CanonicalExists: false, LegacyExists: false));
        _legacyOnly = LegacyReadModelTables.Adjudicate(new(NameFits: true, CanonicalExists: false, LegacyExists: true));
    }

    [Fact] void should_not_refuse_when_both_tables_are_reported() => _bothPresent.ShouldEqual(LegacyReadModelRefusal.None);
    [Fact] void should_not_refuse_when_no_table_exists() => _neither.ShouldEqual(LegacyReadModelRefusal.None);
    [Fact] void should_not_refuse_when_only_one_table_is_reported() => _legacyOnly.ShouldEqual(LegacyReadModelRefusal.None);
}
