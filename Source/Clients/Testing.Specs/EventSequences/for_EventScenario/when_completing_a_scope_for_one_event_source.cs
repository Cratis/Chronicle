// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_completing_a_scope_for_one_event_source : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _covered;
    AppendResult _otherSource;
    AppendResult _otherStream;
    bool _completed;

    void Establish() => _scenario = new();

    async Task Because()
    {
        _completed = (await _scenario.EventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source-a", EventStreamType: "transactions", EventStreamId: "month"))).IsSuccess;
        _covered = await _scenario.EventLog.Append("source-a", new TestEvent("covered"), "transactions", "month");
        _otherSource = await _scenario.EventLog.Append("source-b", new TestEvent("open"), "transactions", "month");
        _otherStream = await _scenario.EventLog.Append("source-a", new TestEvent("open"), "transactions", "next-month");
    }

    [Fact] void should_complete_the_scope() => _completed.ShouldBeTrue();
    [Fact] void should_reject_the_covered_append() => _covered.ShouldHaveConstraintViolation("closed-stream");
    [Fact] void should_accept_the_other_source() => _otherSource.ShouldBeSuccessful();
    [Fact] void should_accept_the_other_stream() => _otherStream.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
