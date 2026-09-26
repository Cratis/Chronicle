// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_seeding_a_read_model_with_per_run_artifacts : Specification
{
    IClientArtifactsProvider _artifacts;
    ReadModelScenario<PerRunReadModel> _scenario;
    EventSourceId _eventSourceId;
    PerRunReadModel _seeded;
    PerRunReadModel _result;

    void Establish()
    {
        _artifacts = PerRunArtifacts.WithEvent();
        var defaults = new Defaults(_artifacts);
        _artifacts.ClearReceivedCalls();
        _scenario = new ReadModelScenario<PerRunReadModel>(null, defaults);
        _eventSourceId = EventSourceId.New();
        _seeded = new PerRunReadModel(Guid.Parse(_eventSourceId.Value), "seeded", null);
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId(_eventSourceId).ReadModel(_seeded);
        _result = await _scenario.ReadModels.GetInstanceById<PerRunReadModel>(_eventSourceId);
    }

    [Fact] void should_return_the_seeded_read_model() => _result.ShouldEqual(_seeded);
    [Fact] void should_use_the_supplied_artifacts_to_create_the_event_store() => _ = _artifacts.Received().EventTypes;
}
