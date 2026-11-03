// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_seeding_an_accepted_event_with_given : Specification, IDisposable
{
    EventScenario _scenario;
    Exception _exception;
    EventCount _stored;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new TestEvent("seeded")));
        _stored = (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_store_the_event() => _stored.ShouldEqual((EventCount)1);

    public void Dispose() => _scenario.Dispose();
}
