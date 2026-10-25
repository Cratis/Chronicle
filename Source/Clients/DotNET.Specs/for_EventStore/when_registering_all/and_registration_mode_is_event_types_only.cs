// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_registration_mode_is_event_types_only : given.an_event_store_with_a_projection_that_cannot_be_built
{
    RegistrationOutcome _outcome;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        SetField("_registrationMode", RegistrationMode.EventTypesOnly);
        SetAutoProperty("Projections", Substitute.For<IProjections>());
    }

    async Task Because()
    {
        await _projections.Discover();
        await _eventStore.RegisterAll();
        _outcome = _eventStore.Registration;
    }

    [Fact] void should_succeed() => _outcome.IsSuccess.ShouldBeTrue();
    [Fact] void should_ensure_the_event_store() => _servicesAccessor.Services.EventStores.Received(1).EnsureEventStore(Arg.Any<Contracts.EventStores.EnsureEventStoreRequest>());
    [Fact] void should_register_event_types() => _eventStore.EventTypes.Received(1).Register();
    [Fact] void should_not_register_event_sources() => _eventStore.EventSources.DidNotReceive().Register();
    [Fact] void should_not_register_constraints() => _eventStore.Constraints.DidNotReceive().Register();
    [Fact] void should_not_register_reactors() => _eventStore.Reactors.DidNotReceive().Register();
    [Fact] void should_not_register_reducers() => _eventStore.Reducers.DidNotReceive().Register();
    [Fact] void should_not_register_projections() => _eventStore.Projections.DidNotReceive().Register();
}
