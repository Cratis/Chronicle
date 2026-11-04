// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_a_routing_argument_contains_a_separator : given.a_query_filter
{
    [Theory]
    [InlineData("eventStore", "#")]
    [InlineData("namespace", "a#b")]
    [InlineData("eventSequenceId", "event-log#System")]
    [InlineData("observerId", "#x")]
    public async Task should_refuse_the_query_with_a_validation_failure(string name, string value)
    {
        var result = await _filter.OnPerform(ContextFor("Cratis.Chronicle.Sequences.Something", ("eventStore", "Store"), (name, value)));
        result.IsValid.ShouldBeFalse();
        result.ValidationResults.Single().Message.ShouldEqual(SystemStoreAccess.SeparatorRefused);
    }

    [Fact]
    async Task should_not_refuse_a_separator_in_a_non_routing_argument()
    {
        var result = await _filter.OnPerform(ContextFor("Cratis.Chronicle.Sequences.Something", ("eventStore", "Store"), ("partition", "a#b")));
        result.ShouldBeNull();
    }
}
