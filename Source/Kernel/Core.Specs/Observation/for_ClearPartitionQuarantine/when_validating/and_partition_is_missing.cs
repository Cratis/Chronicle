// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Observation.for_ClearPartitionQuarantine.when_validating;

public class and_partition_is_missing : Specification
{
    readonly CommandScenario<ClearPartitionQuarantine> _scenario = ChronicleCommandScenario.For<ClearPartitionQuarantine>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new ClearPartitionQuarantine("some-event-store", "some-namespace", "some-observer", "event-log", string.Empty, true));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
