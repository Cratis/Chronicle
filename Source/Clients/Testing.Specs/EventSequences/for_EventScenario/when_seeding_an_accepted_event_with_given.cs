// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_seeding_an_accepted_event_with_given : Specification, IDisposable
{
    EventScenario _scenario;
    Exception _exception;

    void Establish() => _scenario = new EventScenario();

    async Task Because() => _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new TestEvent("seeded")));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();

    public void Dispose() => _scenario.Dispose();
}
