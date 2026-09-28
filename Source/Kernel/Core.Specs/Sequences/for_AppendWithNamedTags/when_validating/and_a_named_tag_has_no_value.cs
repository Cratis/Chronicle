// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Sequences.for_AppendWithNamedTags.when_validating;

public class and_a_named_tag_has_no_value : given.an_append_with_named_tags_validation
{
    async Task Because() => _result = await _scenario.Validate(ValidCommand() with { NamedTags = [new NamedTag("account", null!)] });

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_report_the_rule() => _result.ValidationResults.Select(_ => _.Message).ShouldContain("Named tag value is required.");
}
