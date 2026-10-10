// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_completing_by_event_source_only : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _covered;
    AppendResult _other;
    bool _completed;

    void Establish() => _scenario = new();

    async Task Because()
    {
        _completed = (await _scenario.EventSequence.CompleteStream(ClosedStreamScope.ForEventSource("source-a"))).IsSuccess;
        _covered = await _scenario.EventLog.Append("source-a", new TestEvent("covered"));
        _other = await _scenario.EventLog.Append("source-b", new TestEvent("open"));
    }

    [Fact] void should_complete_the_scope() => _completed.ShouldBeTrue();
    [Fact] void should_reject_the_default_stream_for_the_closed_source() => _covered.ShouldHaveConstraintViolation("closed-stream");
    [Fact] void should_accept_the_other_source() => _other.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
