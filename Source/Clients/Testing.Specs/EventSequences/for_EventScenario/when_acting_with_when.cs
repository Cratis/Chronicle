// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_acting_with_when : Specification, IDisposable
{
    EventScenario _scenario;
    EventSourceId _eventSourceId;
    AppendResult _result;

    void Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();
    }

    async Task Because() =>
        _result = await _scenario.When
            .ForEventSource(_eventSourceId)
            .Events(new TestEvent("acted"));

    [Fact] void should_return_the_append_result() => _result.ShouldNotBeNull();
    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
