// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_a_subscribed_event_yields_an_empty_changeset_with_initial_state : Specification
{
    ReadModelScenario<InitialStateRetainingReadModel> _scenario;
    InitialStateRetainingReadModel _initialState;
    EventSourceId _eventSourceId;

    void Establish()
    {
        var id = Guid.NewGuid();
        _eventSourceId = new EventSourceId(id);
        _initialState = new InitialStateRetainingReadModel(id, "The original model");
        _scenario = new ReadModelScenario<InitialStateRetainingReadModel>(_initialState);
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_eventSourceId)
            .Events(new InitialStateRetainingEvent());

    [Fact] void should_retain_the_initial_instance() => _scenario.Instance.ShouldEqual(_initialState);
}
