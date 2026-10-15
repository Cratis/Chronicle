// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_updating_definition;

public class and_the_sink_is_changed_to_an_event_sequence : given.a_read_models_manager
{
    ReadModelDefinition _existing;
    Exception _error;

    async Task Establish()
    {
        _existing = DefinitionFor("existing", "Existing");
        await _manager.RegisterSingle(_existing);
        _readModelGrain.ClearReceivedCalls();
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.UpdateDefinition(_existing with { Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence) }));

    [Fact] void should_refuse_event_publication() => _error.ShouldBeOfExactType<InconsistentEventSequenceSink>();
    [Fact] async Task should_preserve_existing_registration() => (await _manager.GetDefinitions()).ShouldContainOnly(_existing);
    [Fact] async Task should_not_replace_the_read_model_definition() => await _readModelGrain.DidNotReceive().SetDefinition(Arg.Any<ReadModelDefinition>());
}
