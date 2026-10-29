// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_revising_an_event : Specification
{
    EventScenario _scenario;
    EventSequenceNumber _sequenceNumber;
    Exception _error;
    AppendedEvent _event;

    async Task Establish()
    {
        _scenario = new EventScenario();
        _sequenceNumber = (await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("original"))).SequenceNumber;
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _scenario.EventSequence.Revise(_sequenceNumber, new TestEvent("revised")));
        _event = (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Single();
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_fail_the_command() => _error.ShouldBeOfExactType<CommandFailed>();
    [Fact] void should_report_that_the_operation_is_not_available() => _error.Message.ShouldContain("not available in test scenarios");
    [Fact] void should_leave_the_event_unchanged() => ((TestEvent)_event.Content).Value.ShouldEqual("original");
}
