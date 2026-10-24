// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_the_key_is_ambiguous_but_no_row_exists : given.a_composite_key_lookup_table
{
    async Task Because() => _result = await Lookup("x_y", "z");
    [Fact] void should_not_find_an_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.NotFound);
    [Fact] void should_not_return_an_instance() => _result.Instance.ShouldBeNull();
}
