// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Observation.for_ReplayObserver.when_validating;

public class and_required_values_are_missing : Specification
{
    readonly CommandScenario<ReplayObserver> _scenario = ChronicleCommandScenario.For<ReplayObserver>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new ReplayObserver(string.Empty, string.Empty, string.Empty, string.Empty));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
