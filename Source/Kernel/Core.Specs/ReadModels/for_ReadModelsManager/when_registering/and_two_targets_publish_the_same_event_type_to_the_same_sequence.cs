// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_two_targets_publish_the_same_event_type_to_the_same_sequence : given.a_read_models_manager
{
    Exception _error;
    IEnumerable<ReadModelDefinition> _stored;

    static ReadModelDefinition Publisher(string id, string eventType = "TotalChanged") => DefinitionFor(id, id) with
    {
        Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence, new EventSequenceSinkConfiguration(new EventType(eventType, EventTypeGeneration.First), null, true))
    };

    async Task Because()
    {
        await _manager.Register([Publisher("first"), Publisher("other-type", "Other")]);
        _error = await Catch.Exception(() => _manager.Register([Publisher("second")]));
        _stored = await _manager.GetDefinitions();
    }

    [Fact] void should_refuse_the_second_publisher() => _error.ShouldBeOfExactType<EventPublisherConflict>();
    [Fact] void should_keep_the_first_registration() => _stored.Select(_ => _.Identifier.Value).Order().ShouldEqual(["first", "other-type"]);
}
