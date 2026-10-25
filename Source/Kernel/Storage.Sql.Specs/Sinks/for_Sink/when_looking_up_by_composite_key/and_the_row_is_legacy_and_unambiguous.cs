// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_the_row_is_legacy_and_unambiguous : given.a_composite_key_lookup_table
{
    string? _stamp;
    async Task Establish()
    {
        await Write("a", "b", "legacy");
        await SetStamp(null);
    }
    async Task Because()
    {
        _result = await Lookup("a", "b");
        await using var context = CreateContext();
        _stamp = await context.Database.SqlQuery<string?>($"SELECT __compositeKey AS Value FROM composite_shadows").SingleAsync();
    }
    [Fact] void should_find_the_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_not_stamp_on_read() => _stamp.ShouldBeNull();
}
