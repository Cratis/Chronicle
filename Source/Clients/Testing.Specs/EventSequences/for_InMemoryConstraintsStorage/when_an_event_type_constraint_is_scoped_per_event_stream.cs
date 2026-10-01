// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_InMemoryConstraintsStorage;

/// <summary>
/// A once-per-event-source constraint scoped per event stream is enforced within a stream and not across streams. Were
/// the scope dropped on the way to the in-process kernel, the constraint would apply to the event source as a whole and
/// the append in the second stream would be refused.
/// </summary>
public class when_an_event_type_constraint_is_scoped_per_event_stream : Specification, IDisposable
{
    static readonly EventStreamType _streamType = new("rota");
    static readonly EventSourceId _employee = EventSourceId.New();

    EventScenario _scenario;
    AppendResult _firstInFirstStream;
    AppendResult _secondInFirstStream;
    AppendResult _firstInSecondStream;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ScopedShiftStarted)]);
        artifacts.ConstraintTypes.Returns([typeof(OneShiftPerEmployeeAndStream)]);
        _scenario = new EventScenario(new Defaults(artifacts));
    }

    async Task Because()
    {
        _firstInFirstStream = await _scenario.EventLog.Append(_employee, new ScopedShiftStarted("Warehouse"), _streamType, new EventStreamId("monday"));
        _secondInFirstStream = await _scenario.EventLog.Append(_employee, new ScopedShiftStarted("Warehouse"), _streamType, new EventStreamId("monday"));
        _firstInSecondStream = await _scenario.EventLog.Append(_employee, new ScopedShiftStarted("Warehouse"), _streamType, new EventStreamId("tuesday"));
    }

    [Fact] void should_accept_the_first_event_in_the_first_stream() => _firstInFirstStream.ShouldBeSuccessful();
    [Fact] void should_reject_a_second_event_in_the_same_stream() => _secondInFirstStream.ShouldHaveConstraintViolation(OneShiftPerEmployeeAndStream.Name);
    [Fact] void should_accept_an_event_in_another_stream() => _firstInSecondStream.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
