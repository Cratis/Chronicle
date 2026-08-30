// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<EnsureEventStore> _scenario = ChronicleCommandScenario.For<EnsureEventStore>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new EnsureEventStore("some-event-store"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
