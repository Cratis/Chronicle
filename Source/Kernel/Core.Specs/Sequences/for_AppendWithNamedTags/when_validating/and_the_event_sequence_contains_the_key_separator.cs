// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendWithNamedTags.when_validating;

public class and_the_event_sequence_contains_the_key_separator : given.an_append_with_named_tags_validation
{
    async Task Because() => _result = await _scenario.Validate(ValidCommand() with { EventSequenceId = "event-log#System#Default" });

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_event_sequence_member() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventSequenceId");
    [Fact] void should_report_the_separator_rule() => _result.ValidationResults.Select(result => result.Message).ShouldContain("The value must not contain '#'.");
}
