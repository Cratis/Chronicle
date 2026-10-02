// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Observation.for_ClearPartitionQuarantine.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<ClearPartitionQuarantine> _scenario = ChronicleCommandScenario.For<ClearPartitionQuarantine>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new ClearPartitionQuarantine("some-event-store", "some-namespace", "some-observer", "event-log", "some-partition", true));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
