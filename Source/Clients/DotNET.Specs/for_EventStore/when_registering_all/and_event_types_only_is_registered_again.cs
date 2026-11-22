// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_event_types_only_is_registered_again : given.an_event_store_with_a_projection_that_cannot_be_built
{
    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        SetField("_registrationMode", RegistrationMode.EventTypesOnly);
    }

    async Task Because()
    {
        await _projections.Discover();
        await _eventStore.RegisterAll();
        await _eventStore.RegisterAll();
    }

    [Fact] void should_register_types_each_time() => _eventStore.EventTypes.Received(2).Register();
    [Fact] void should_not_register_read_models() => _eventStore.ReadModels.DidNotReceive().Register();
    [Fact] void should_not_start_read_model_reactors() => _eventStore.ReadModelReactors.DidNotReceive().Start();
    [Fact] void should_not_seed_events() => _eventStore.Seeding.DidNotReceive().Register();
    [Fact] void should_not_register_subscriptions() => _eventStore.Subscriptions.ReceivedCalls().ShouldBeEmpty();
}
