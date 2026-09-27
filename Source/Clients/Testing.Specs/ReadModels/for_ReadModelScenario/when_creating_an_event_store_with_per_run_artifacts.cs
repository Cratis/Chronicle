// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_creating_an_event_store_with_per_run_artifacts : Specification
{
    IClientArtifactsProvider _artifacts;
    ReadModelScenario<PerRunModel> _scenario;

    void Establish()
    {
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.EventTypes.Returns([typeof(PerRunEvent)]);
        _scenario = new ReadModelScenario<PerRunModel>(null, new Defaults(_artifacts));
        _artifacts.ClearReceivedCalls();
    }

    void Because() => _ = _scenario.ReadModels;

    [Fact] void should_discover_events_from_the_given_artifacts() => _ = _artifacts.Received().EventTypes;
}
