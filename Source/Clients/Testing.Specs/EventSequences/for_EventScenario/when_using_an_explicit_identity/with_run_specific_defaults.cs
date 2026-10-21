// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_using_an_explicit_identity;

public class with_run_specific_defaults : Specification
{
    EventScenario _scenario;
    AppendResult _duplicate;
    string _handle;

    async Task Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(DiscoveredHandleClaimed)]);
        artifacts.UniqueConstraints.Returns([typeof(DiscoveredHandleClaimed)]);
        _scenario = new(new Defaults(artifacts), EventSequenceId.Log, "run-store", "run-namespace");
        _handle = Guid.NewGuid().ToString();
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new DiscoveredHandleClaimed(_handle));
    }

    async Task Because() => _duplicate = await _scenario.EventLog.Append(EventSourceId.New(), new DiscoveredHandleClaimed(_handle));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_discover_from_the_supplied_registry() => _duplicate.ShouldHaveConstraintViolationFor(DiscoveredHandleClaimed.ConstraintName);
}
