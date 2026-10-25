// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlCompositeKeyLookup.when_deciding;

public class and_no_row_matches : Specification
{
    (CompositeKeyLookupOutcome Outcome, string? IdToLoad) _result;
    void Because() => _result = SqlCompositeKeyLookup.Decide([], "~k1:requested", "legacy", true);
    [Fact] void should_not_find_a_row() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.NotFound);
    [Fact] void should_not_load_a_row() => _result.IdToLoad.ShouldBeNull();
}
