// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_appending_through_an_event_source;

public class and_the_event_source_is_not_discovered : Specification, IDisposable
{
    EventScenario _scenario;
    Exception _exception;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(TestEvent)]);
        _scenario = new EventScenario(new Defaults(artifacts));
    }

    async Task Because() => _exception = await Catch.Exception(() => _scenario.EventLog.Append<WarehouseEventSource>("pallet-1", new TestEvent("hello")));

    [Fact] void should_reject_the_append() => _exception.ShouldBeOfExactType<UnknownEventSource>();

    public void Dispose() => _scenario.Dispose();
}
