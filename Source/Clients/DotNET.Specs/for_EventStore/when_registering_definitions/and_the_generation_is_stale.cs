// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_the_generation_is_stale : given.an_event_store_with_buildable_definitions
{
    void Establish() => _services.EventStores.RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>()).Returns(new CommandResult
    {
        ValidationResults = [new() { Members = ["GeNeRaTiOn"], Message = "The stored deployment generation is newer." }]
    });

    async Task Because() => _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions(new DeploymentGeneration(7)));

    [Fact]
    void should_report_a_stale_generation() => _failure.ShouldBeOfExactType<DeploymentGenerationIsStale>();

    [Fact]
    void should_include_the_kernel_reason() => _failure.Message.Contains("The stored deployment generation is newer.").ShouldBeTrue();

    [Fact]
    void should_publish_the_failure() => _eventStore.Registration.Failure.ShouldBeSame(_failure);

    [Fact]
    void should_not_send_any_artifacts()
    {
        _eventStore.EventTypes.DidNotReceive().Register();
        _eventStore.EventSources.DidNotReceive().Register();
        _eventStore.ReadModels.DidNotReceive().Register();
        _eventStore.Constraints.DidNotReceive().Register();
        _services.Projections.DidNotReceive().Register(Arg.Any<Contracts.Projections.RegisterRequest>());
    }
}
