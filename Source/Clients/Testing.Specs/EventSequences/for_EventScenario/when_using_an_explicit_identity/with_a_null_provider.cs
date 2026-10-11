// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_using_an_explicit_identity;

public class with_a_null_provider : Specification
{
    EventScenario _scenario;
    AppendResult _duplicate;
    string _handle;

    async Task Establish()
    {
        _scenario = new(EventSequenceId.Log, "explicit-store", "explicit-namespace", null);
        _handle = Guid.NewGuid().ToString();
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new DiscoveredHandleClaimed(_handle));
    }

    async Task Because() => _duplicate = await _scenario.EventLog.Append(EventSourceId.New(), new DiscoveredHandleClaimed(_handle));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_keep_constraints_disabled() => _duplicate.ShouldBeSuccessful();
}
