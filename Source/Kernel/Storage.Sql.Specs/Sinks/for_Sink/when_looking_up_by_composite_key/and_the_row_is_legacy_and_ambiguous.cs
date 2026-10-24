// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_the_row_is_legacy_and_ambiguous : given.a_composite_key_lookup_table
{
    async Task Establish()
    {
        await Write("x_y", "z", "legacy");
        await SetStamp(null);
    }
    async Task Because() => _result = await Lookup("x_y", "z");
    [Fact] void should_report_ambiguity() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.AmbiguousLegacyKey);
    [Fact] void should_not_return_an_instance() => _result.Instance.ShouldBeNull();
}
