// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key;

[Collection(PostgreSqlCollection.Name)]
public class and_rows_are_legacy_stamped_and_colliding_on_postgresql(PostgreSqlFixture fixture) : given.a_composite_key_lookup_table
{
    CompositeKeyLookupResult[] _results;
    protected override ISqlSinkHarness CreateHarness() => new PostgreSqlSinkHarness { Fixture = fixture };
    async Task Because() => _results = await LookupLegacyStampedAndCollidingRows();
    [Fact] void should_find_the_unambiguous_legacy_row() => _results[0].Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_report_legacy_ambiguity() => _results[1].Outcome.ShouldEqual(CompositeKeyLookupOutcome.AmbiguousLegacyKey);
    [Fact] void should_find_the_stamped_row() => _results[2].Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_report_the_collision() => _results[3].Outcome.ShouldEqual(CompositeKeyLookupOutcome.KeyCollision);
}
