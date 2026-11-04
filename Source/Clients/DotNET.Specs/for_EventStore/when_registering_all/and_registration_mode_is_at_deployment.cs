// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_registration_mode_is_at_deployment : given.an_event_store_with_a_projection_that_cannot_be_built
{
    RegistrationOutcome _outcome;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        SetField("_registrationMode", RegistrationMode.AtDeployment);
        SetAutoProperty("Projections", Substitute.For<IProjections>());
    }

    async Task Because()
    {
        await _projections.Discover();
        await _eventStore.RegisterAll();
        _outcome = _eventStore.Registration;
    }

    [Fact] void should_complete_the_registration() => _outcome.HasRun.ShouldBeTrue();
    [Fact] void should_succeed() => _outcome.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_ensure_the_event_store() => _servicesAccessor.Services.EventStores.DidNotReceive().EnsureEventStore(Arg.Any<Contracts.EventStores.EnsureEventStoreRequest>());
    [Fact] void should_not_register_event_types() => _eventStore.EventTypes.DidNotReceive().Register();
    [Fact] void should_not_register_projections() => _eventStore.Projections.DidNotReceive().Register();
}
