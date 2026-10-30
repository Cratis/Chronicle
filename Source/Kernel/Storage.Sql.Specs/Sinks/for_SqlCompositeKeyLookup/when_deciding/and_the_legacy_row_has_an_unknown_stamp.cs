// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlCompositeKeyLookup.when_deciding;

public class and_the_legacy_row_has_an_unknown_stamp : Specification
{
    (CompositeKeyLookupOutcome Outcome, string? IdToLoad) _result;
    void Because() => _result = SqlCompositeKeyLookup.Decide([new() { Id = "legacy", Stamp = "~k9:future" }], "~k1:requested", "legacy", false);
    [Fact] void should_find_the_unambiguous_row() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_load_the_legacy_id() => _result.IdToLoad.ShouldEqual("legacy");
}
