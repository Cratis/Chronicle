// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_a_projection_cannot_be_built : when_registering_all.given.an_event_store_with_a_projection_that_cannot_be_built
{
    Exception _failure;

    async Task Because() => _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions(new DeploymentGeneration(1)));

    [Fact]
    void should_report_the_unbuildable_artifacts() => _failure.ShouldBeOfExactType<ArtifactDefinitionsIncomplete>();

    [Fact]
    void should_identify_the_failed_projection() => ((ArtifactDefinitionsIncomplete)_failure).Unregistered.Single().ArtifactType.ShouldEqual(typeof(UnbuildableProjection));

    [Fact]
    void should_not_ensure_the_event_store() => _servicesAccessor.Services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<Contracts.EventStores.EnsureEventStoreRequest>());

    [Fact]
    void should_not_record_a_deployment() => _servicesAccessor.Services.EventStores.DidNotReceive().RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>());

    [Fact]
    void should_not_send_artifacts()
    {
        _eventStore.EventTypes.DidNotReceive().Register();
        _eventStore.ReadModels.DidNotReceive().Register();
        _servicesAccessor.Services.Projections.DidNotReceive().Register(Arg.Any<Contracts.Projections.RegisterRequest>());
    }
}
