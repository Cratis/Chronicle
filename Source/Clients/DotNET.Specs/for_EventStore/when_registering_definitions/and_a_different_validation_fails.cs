// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_a_different_validation_fails : given.an_event_store_with_buildable_definitions
{
    void Establish() => _services.EventStores.RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>()).Returns(new CommandResult
    {
        ValidationResults = [new() { Members = ["fingerprint"], Message = "Invalid fingerprint" }]
    });

    async Task Because() => _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions(new DeploymentGeneration(1)));

    [Fact]
    void should_preserve_the_wire_validation_exception() => _failure.ShouldBeOfExactType<CommandFailed>();

    [Fact]
    void should_not_send_artifacts() => _eventStore.EventTypes.DidNotReceive().Register();
}
