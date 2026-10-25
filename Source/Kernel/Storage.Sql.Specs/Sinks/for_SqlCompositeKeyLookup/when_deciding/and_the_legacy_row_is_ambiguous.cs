// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlCompositeKeyLookup.when_deciding;

public class and_the_legacy_row_is_ambiguous : Specification
{
    (CompositeKeyLookupOutcome Outcome, string? IdToLoad) _result;
    void Because() => _result = SqlCompositeKeyLookup.Decide([new() { Id = "legacy" }], "~k1:requested", "legacy", true);
    [Fact] void should_report_ambiguity() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.AmbiguousLegacyKey);
    [Fact] void should_not_load_a_row() => _result.IdToLoad.ShouldBeNull();
}
