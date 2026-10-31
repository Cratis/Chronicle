// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSourcesWithNamedTags.when_validating;

public class and_the_event_store_is_the_system_event_store_in_uppercase : given.an_append_many_for_event_sources_with_named_tags_validation
{
    async Task Because() => _result = await _scenario.Validate(ValidCommand() with { EventStore = "SYSTEM" });

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_event_store_member() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventStore");
    [Fact] void should_report_the_reserved_store_rule() => _result.ValidationResults.Select(result => result.Message).ShouldContain("The System event store is reserved for the kernel and cannot be written through the public API.");
}
