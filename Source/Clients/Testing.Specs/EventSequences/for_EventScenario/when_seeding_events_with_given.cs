// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_seeding_events_with_given : Specification, IDisposable
{
    EventScenario _scenario;
    EventSourceId _eventSourceId;
    AppendResult _result;

    void Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_eventSourceId)
            .Events(new TestEvent("seeded"));

        _result = await _scenario.EventLog.Append(
            _eventSourceId,
            new TestEvent("appended after seed"));
    }

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
