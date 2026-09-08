// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Security.for_AddApplication.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<AddApplication> _scenario = ChronicleCommandScenario.For<AddApplication>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new AddApplication(Guid.NewGuid(), "some-client", "some-secret"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
