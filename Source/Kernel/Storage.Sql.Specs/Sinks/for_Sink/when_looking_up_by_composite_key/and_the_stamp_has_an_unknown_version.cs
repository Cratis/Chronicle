// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_the_stamp_has_an_unknown_version : given.a_composite_key_lookup_table
{
    CompositeKeyLookupResult _ambiguous;
    async Task Establish()
    {
        await Write("a", "b", "legacy");
        await Write("x_y", "z", "legacy");
        await SetStamp("~k9:future");
    }
    async Task Because()
    {
        _result = await Lookup("a", "b");
        _ambiguous = await Lookup("x_y", "z");
    }
    [Fact] void should_find_the_unambiguous_legacy_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_report_the_ambiguous_legacy_instance() => _ambiguous.Outcome.ShouldEqual(CompositeKeyLookupOutcome.AmbiguousLegacyKey);
}
