// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LegacyReadModelTables.when_adjudicating;

public class and_a_long_name_has_neither_table : Specification
{
    LegacyReadModelRefusal _result;

    void Because() => _result = LegacyReadModelTables.Adjudicate(new(NameFits: false, CanonicalExists: false, LegacyExists: false));

    // #4340: refusal pending an L0-approved fresh-scope authority; absence of both tables is not proof of a fresh scope.
    [Fact] void should_refuse_because_historical_ownership_is_unprovable() => _result.ShouldEqual(LegacyReadModelRefusal.HistoricalOwnershipUnprovable);
}
