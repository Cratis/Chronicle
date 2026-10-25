// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_the_row_is_stamped : given.a_composite_key_lookup_table
{
    async Task Establish() => await Write("x_y", "z", "stamped");
    async Task Because() => _result = await Lookup("x_y", "z");
    [Fact] void should_find_the_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_return_the_instance() => ((IDictionary<string, object?>)_result.Instance!)["name"].ShouldEqual("stamped");
}
