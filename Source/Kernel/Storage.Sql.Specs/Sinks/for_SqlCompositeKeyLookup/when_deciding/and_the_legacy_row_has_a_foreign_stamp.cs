// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlCompositeKeyLookup.when_deciding;

public class and_the_legacy_row_has_a_foreign_stamp : Specification
{
    (CompositeKeyLookupOutcome Outcome, string? IdToLoad) _result;
    void Because() => _result = SqlCompositeKeyLookup.Decide([new() { Id = "legacy", Stamp = "~k1:foreign" }], "~k1:requested", "legacy", false);
    [Fact] void should_report_a_collision() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.KeyCollision);
    [Fact] void should_not_load_a_row() => _result.IdToLoad.ShouldBeNull();
}
