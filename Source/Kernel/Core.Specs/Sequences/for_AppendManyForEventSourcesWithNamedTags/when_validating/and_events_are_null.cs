// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSourcesWithNamedTags.when_validating;

public class and_events_are_null : given.an_append_many_for_event_sources_with_named_tags_validation
{
    async Task Because()
    {
        // Executed rather than only validated: the rejection must come from validation before the handler dereferences the batch.
        _result = await _scenario.Execute(ValidCommand() with { Events = null! });
    }

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_report_the_rule() => _result.ValidationResults.Select(_ => _.Message).ShouldContain("At least one event is required.");
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
}
