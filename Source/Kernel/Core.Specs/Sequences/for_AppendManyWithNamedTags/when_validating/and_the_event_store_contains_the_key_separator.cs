// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendManyWithNamedTags.when_validating;

public class and_the_event_store_contains_the_key_separator : given.an_append_many_with_named_tags_validation
{
    async Task Because() => _result = await _scenario.Validate(ValidCommand() with { EventStore = "x#System" });

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_event_store_member() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventStore");
    [Fact] void should_report_the_separator_rule() => _result.ValidationResults.Select(result => result.Message).ShouldContain("The value must not contain '#'.");
}
