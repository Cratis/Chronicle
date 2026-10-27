// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_the_generation_is_out_of_range : given.an_event_store_with_buildable_definitions
{
    async Task Because() => _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions(new DeploymentGeneration(long.MaxValue + 1UL)));

    [Fact]
    void should_report_the_signed_storage_limit() => _failure.ShouldBeOfExactType<DeploymentGenerationOutOfRange>();

    [Fact]
    void should_fail_before_waiting_for_services() => _ = _servicesAccessor.DidNotReceive().Services;

    [Fact]
    void should_not_send_a_deployment() => _services.EventStores.DidNotReceive().RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>());
}
