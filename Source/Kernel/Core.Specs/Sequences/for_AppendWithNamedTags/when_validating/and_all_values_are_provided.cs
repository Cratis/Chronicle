// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Sequences.for_AppendWithNamedTags.when_validating;

public class and_all_values_are_provided : given.an_append_with_named_tags_validation
{
    async Task Because() => _result = await _scenario.Validate(ValidCommand());

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
