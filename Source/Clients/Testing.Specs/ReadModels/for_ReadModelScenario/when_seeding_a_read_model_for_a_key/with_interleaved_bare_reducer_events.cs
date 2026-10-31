// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class with_interleaved_bare_reducer_events : appended_contexts
{
    ReadModelScenario<ScenarioContextReduction> _scenario;

    void Establish()
    {
        _scenario = new();
        _scenario.Given.ForEventSourceId(_firstId).ReadModel(new([]));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId(_secondId).Events(new ScenarioContextRecorded("other first"));
        await _scenario.Given.ForEventSourceId(_firstId).Events(new ScenarioContextRecorded("seeded first"));
        await _scenario.Given.ForEventSourceId(_secondId).Events(new ScenarioContextRecorded("other second"));
        await _scenario.Given.ForEventSourceId(_firstId).Events(new ScenarioContextRecorded("seeded second"));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_preserve_collection_positions_for_the_seeded_key() => _scenario.InstanceForEventSourceId(_firstId)!.Contexts.Select(context => context.SequenceNumber.Value).ShouldEqual([1UL, 3UL]);
    [Fact] void should_preserve_collection_positions_for_the_unseeded_key() => _scenario.InstanceForEventSourceId(_secondId)!.Contexts.Select(context => context.SequenceNumber.Value).ShouldEqual([0UL, 2UL]);
}
