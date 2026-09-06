// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// "The instance does not exist" is one of the two branches of every read-model guard - a command that checks
/// <c>IReadModels.GetInstanceById</c> is null before proceeding relies on exactly this. Nothing seeded under an
/// id used to fall through to a grain that is not available in test scenarios, throwing instead of resolving to
/// null the way the interface's nullable return promises. Seeding a different id first did not help either -
/// the lookup for the unseeded id still went to the grain and still threw (#3927).
/// </summary>
public class when_reading_an_unseeded_instance_through_the_read_models_surface : Specification
{
    ReadModelScenario<SimpleModule> _emptyScenario;
    ReadModelScenario<SimpleModule> _scenarioWithADifferentIdSeeded;
    EventSourceId _seededId;
    EventSourceId _unseededId;
    SimpleModule _resultFromEmptyScenario;
    SimpleModule _resultFromScenarioWithADifferentIdSeeded;

    void Establish()
    {
        _emptyScenario = new ReadModelScenario<SimpleModule>();
        _scenarioWithADifferentIdSeeded = new ReadModelScenario<SimpleModule>();
        _seededId = EventSourceId.New();
        _unseededId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenarioWithADifferentIdSeeded.Given.ForEventSource(_seededId).Events(new ModuleCreated("My Module"));

        _resultFromEmptyScenario = await _emptyScenario.ReadModels.GetInstanceById<SimpleModule>(_unseededId);
        _resultFromScenarioWithADifferentIdSeeded = await _scenarioWithADifferentIdSeeded.ReadModels.GetInstanceById<SimpleModule>(_unseededId);
    }

    [Fact] void should_resolve_to_null_when_nothing_was_seeded() => _resultFromEmptyScenario.ShouldBeNull();
    [Fact] void should_resolve_to_null_when_a_different_id_was_seeded() => _resultFromScenarioWithADifferentIdSeeded.ShouldBeNull();
}
