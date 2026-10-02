// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_through_an_event_source;

public class given_a_scenario_with_an_event_source : Specification, IDisposable
{
    protected EventScenario _scenario;
    protected readonly EventSourceId _source = "pallet-1";

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(TestEvent)]);
        artifacts.EventSources.Returns([typeof(WarehouseEventSource)]);
        _scenario = new EventScenario(new Defaults(artifacts));
    }

    public void Dispose() => _scenario.Dispose();
}
