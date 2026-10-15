// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_a_publisher_registers_again : given.a_read_models_manager
{
    Exception _error;

    async Task Because()
    {
        var publisher = DefinitionFor("first", "first") with
        {
            Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence, new EventSequenceSinkConfiguration(new EventType("TotalChanged", EventTypeGeneration.First), null, true))
        };
        await _manager.Register([publisher]);
        _error = await Catch.Exception(() => _manager.Register([publisher]));
    }

    [Fact] void should_not_refuse_the_same_publisher() => _error.ShouldBeNull();
}
