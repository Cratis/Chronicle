// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreCommandFilter;

public class when_a_routing_value_contains_a_separator : given.a_command_filter
{
    public static TheoryData<string, string, string, string> Variants() => new()
    {
        { "#", "Default", "observer", "event-log" },
        { "Store", "a#b", "observer", "event-log" },
        { "Store", "Default", "#System", "event-log" },
        { "Store", "Default", "observer", "event-log#System#Default" }
    };

    [Theory]
    [MemberData(nameof(Variants))]
    public async Task should_refuse_the_command_with_a_validation_failure(string eventStore, string @namespace, string observerId, string sequence)
    {
        var result = await _filter.OnExecution(ContextFor(Retry(eventStore, @namespace, observerId, sequence)));
        result.IsValid.ShouldBeFalse();
        result.ValidationResults.Single().Message.ShouldEqual(SystemStoreAccess.SeparatorRefused);
    }

    [Fact]
    async Task should_not_refuse_a_separator_in_user_data()
    {
        var result = await _filter.OnExecution(ContextFor(Retry() with { Partition = "a#b" }));
        result.ShouldBeNull();
    }
}
