// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Namespaces.for_EnsureNamespace.when_validating;

public class and_the_namespace_contains_a_comma : Specification
{
    readonly CommandScenario<EnsureNamespace> _scenario = ChronicleCommandScenario.For<EnsureNamespace>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new EnsureNamespace("some-event-store", "raalabs,raalabs"));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
}
