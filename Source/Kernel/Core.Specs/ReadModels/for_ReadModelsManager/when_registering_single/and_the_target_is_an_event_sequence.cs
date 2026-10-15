// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering_single;

public class and_the_target_is_an_event_sequence : given.a_read_models_manager
{
    ReadModelDefinition _target;
    Exception _error;

    void Establish() => _target = DefinitionFor("event-target", "Event target") with { Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence) };
    async Task Because() => _error = await Catch.Exception(() => _manager.RegisterSingle(_target));

    [Fact] void should_refuse_event_publication() => _error.ShouldBeOfExactType<InconsistentEventSequenceSink>();
    [Fact] async Task should_leave_registration_empty() => (await _manager.GetDefinitions()).ShouldBeEmpty();
    [Fact] async Task should_not_write_a_read_model_definition() => await _readModelGrain.DidNotReceive().SetDefinition(Arg.Any<ReadModelDefinition>());
}
