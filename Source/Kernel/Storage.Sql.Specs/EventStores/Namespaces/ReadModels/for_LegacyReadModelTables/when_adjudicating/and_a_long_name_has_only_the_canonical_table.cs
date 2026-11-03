// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LegacyReadModelTables.when_adjudicating;

public class and_a_long_name_has_only_the_canonical_table : Specification
{
    LegacyReadModelRefusal _result;

    void Because() => _result = LegacyReadModelTables.Adjudicate(new(NameFits: false, CanonicalExists: true, LegacyExists: false));

    // #4340: historical shared-table ownership is unprovable without an approved authority, so the existing canonical table is not trusted.
    [Fact] void should_refuse_because_historical_ownership_is_unprovable() => _result.ShouldEqual(LegacyReadModelRefusal.HistoricalOwnershipUnprovable);
}
