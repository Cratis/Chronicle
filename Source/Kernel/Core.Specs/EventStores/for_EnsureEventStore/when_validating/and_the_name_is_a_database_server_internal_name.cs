// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_validating;

public class and_the_name_is_a_database_server_internal_name : Specification
{
    readonly CommandScenario<EnsureEventStore> _scenario = ChronicleCommandScenario.For<EnsureEventStore>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new EnsureEventStore("ADMIN"));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
}
