// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_asking_if_a_scope_is_completed : Specification, IDisposable
{
    EventScenario _scenario;
    bool _covered;
    bool _open;

    async Task Establish()
    {
        _scenario = new();
        (await _scenario.EventSequence.CompleteStream(ClosedStreamScope.ForEventSource("source-a"))).IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        _covered = await _scenario.EventSequence.IsStreamCompleted(new ClosedStreamScope(EventSourceId: "source-a", EventStreamId: "month"));
        _open = await _scenario.EventSequence.IsStreamCompleted(ClosedStreamScope.ForEventSource("source-b"));
    }

    [Fact] void should_report_the_covered_scope() => _covered.ShouldBeTrue();
    [Fact] void should_report_the_other_scope_as_open() => _open.ShouldBeFalse();

    public void Dispose() => _scenario.Dispose();
}
