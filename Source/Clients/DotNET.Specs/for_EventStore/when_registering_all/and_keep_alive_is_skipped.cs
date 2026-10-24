// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_keep_alive_is_skipped : given.an_event_store_with_a_projection_that_cannot_be_built
{
    RegistrationOutcome _outcome;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        var connection = Substitute.For<IChronicleConnection, IConnectionKeepAlive>();
        connection.Lifecycle.Returns(_connectionLifecycle);
        ((IConnectionKeepAlive)connection).IsSkipped.Returns(true);
        SetAutoProperty("Connection", connection);
    }

    async Task Because()
    {
        await _projections.Discover();
        await _eventStore.RegisterAll();
        _outcome = _eventStore.Registration;
    }

    [Fact] void should_succeed() => _outcome.IsSuccess.ShouldBeTrue();
    [Fact] void should_register_event_types() => _eventStore.EventTypes.Received(1).Register();
    [Fact] void should_register_event_sources() => _eventStore.EventSources.Received(1).Register();
    [Fact] void should_register_constraints() => _eventStore.Constraints.Received(1).Register();
    [Fact] void should_register_the_projection() => _outcome.Artifacts.Select(_ => _.ArtifactType).ShouldContainOnly([typeof(BuildableProjection)]);
    [Fact] void should_not_register_reactors() => _eventStore.Reactors.DidNotReceive().Register();
    [Fact] void should_not_register_reducers() => _eventStore.Reducers.DidNotReceive().Register();
    [Fact] void should_not_start_read_model_reactors() => _eventStore.ReadModelReactors.DidNotReceive().Start();
}
