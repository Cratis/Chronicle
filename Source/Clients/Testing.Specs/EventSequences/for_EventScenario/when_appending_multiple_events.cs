// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_appending_multiple_events : Specification, IDisposable
{
    EventScenario _scenario;
    AppendManyResult _result;

    void Establish() => _scenario = new EventScenario();

    async Task Because() => _result = await _scenario.EventLog.AppendMany(
        EventSourceId.New(),
        [new TestEvent("first"), new TestEvent("second")]);

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
