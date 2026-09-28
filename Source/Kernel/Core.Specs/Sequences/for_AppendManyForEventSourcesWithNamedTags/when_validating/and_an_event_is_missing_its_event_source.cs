// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSourcesWithNamedTags.when_validating;

public class and_an_event_is_missing_its_event_source : given.an_append_many_for_event_sources_with_named_tags_validation
{
    async Task Because() => _result = await _scenario.Validate(ValidCommand() with { Events = [EventFor(string.Empty, new NamedTag("account", "one"))] });

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_report_the_rule() => _result.ValidationResults.Select(_ => _.Message).ShouldContain("Event source identifier is required.");
}
