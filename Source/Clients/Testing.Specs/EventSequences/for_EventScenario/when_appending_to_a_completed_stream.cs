// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Completing a stream closes it for further appends while leaving every other stream open. The harness has to
/// agree with the kernel's closed-stream storage here or specs accept an append the real event store rejects.
/// </summary>
public class when_appending_to_a_completed_stream : Specification, IDisposable
{
    const string ClosedStreamConstraint = "closed-stream";

    static readonly EventStreamType _streamType = new("conversation");
    static readonly EventStreamId _completedStream = new("the-completed-one");
    static readonly EventStreamId _openStream = new("the-open-one");

    EventScenario _scenario;
    AppendResult _beforeCompletion;
    AppendResult _afterCompletion;
    AppendResult _toAStreamStillOpen;
    bool _completed;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        _beforeCompletion = await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("before"), _streamType, _completedStream);
        _completed = (await _scenario.EventSequence.CompleteStream(_streamType, _completedStream)).IsSuccess;
        _afterCompletion = await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("after"), _streamType, _completedStream);
        _toAStreamStillOpen = await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("elsewhere"), _streamType, _openStream);
    }

    [Fact] void should_accept_the_append_before_completion() => _beforeCompletion.ShouldBeSuccessful();
    [Fact] void should_complete_the_stream() => _completed.ShouldBeTrue();
    [Fact] void should_reject_the_append_to_the_completed_stream() => _afterCompletion.ShouldHaveConstraintViolation(ClosedStreamConstraint);
    [Fact] void should_accept_the_append_to_a_stream_still_open() => _toAStreamStillOpen.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
