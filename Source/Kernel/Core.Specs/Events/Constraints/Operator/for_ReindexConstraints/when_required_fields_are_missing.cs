// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints;

public class when_required_fields_are_missing : given.a_reindex_command
{
    async Task Because() => _result = await _scenario.Validate(new(string.Empty, string.Empty, string.Empty, []));

    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
