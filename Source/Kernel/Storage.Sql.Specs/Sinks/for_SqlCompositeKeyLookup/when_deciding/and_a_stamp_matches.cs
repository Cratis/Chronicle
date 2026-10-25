// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlCompositeKeyLookup.when_deciding;

public class and_a_stamp_matches : Specification
{
    (CompositeKeyLookupOutcome Outcome, string? IdToLoad) _result;
    void Because() => _result = SqlCompositeKeyLookup.Decide([new() { Id = "legacy", Stamp = "~k1:foreign" }, new() { Id = "other", Stamp = "~k1:requested" }], "~k1:requested", "legacy", true);
    [Fact] void should_prefer_the_stamp() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_load_its_row() => _result.IdToLoad.ShouldEqual("other");
}
