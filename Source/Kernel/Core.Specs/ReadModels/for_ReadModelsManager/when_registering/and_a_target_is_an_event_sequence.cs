// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_a_target_is_an_event_sequence : given.a_read_models_manager
{
    ReadModelDefinition _readModel;
    ReadModelDefinition _eventTarget;
    Exception _error;

    void Establish()
    {
        _readModel = DefinitionFor("read-model", "Read model");
        _eventTarget = DefinitionFor("event-target", "Event target") with
        {
            Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence)
        };
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.Register([_readModel, _eventTarget]));

    [Fact] void should_refuse_event_publication() => _error.ShouldBeOfExactType<InconsistentEventSequenceSink>();
    [Fact] async Task should_not_register_any_definition() => (await _manager.GetDefinitions()).ShouldBeEmpty();
    [Fact] async Task should_not_write_individual_definitions() => await _readModelGrain.DidNotReceive().SetDefinition(Arg.Any<ReadModelDefinition>());
}
