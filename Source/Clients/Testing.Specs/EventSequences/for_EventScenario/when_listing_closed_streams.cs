// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_listing_closed_streams : Specification, IDisposable
{
    EventScenario _scenario;
    IImmutableList<ClosedStream> _all;
    IImmutableList<ClosedStream> _filtered;

    async Task Establish()
    {
        _scenario = new();
        (await _scenario.EventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source-a", EventStreamId: "month"))).IsSuccess.ShouldBeTrue();
        (await _scenario.EventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source-b", EventStreamId: "month"))).IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        _all = await _scenario.EventSequence.GetClosedStreams();
        _filtered = await _scenario.EventSequence.GetClosedStreams(ClosedStreamScope.ForEventSource("source-a"));
    }

    [Fact] void should_list_both_closures() => _all.Count.ShouldEqual(2);
    [Fact] void should_filter_by_source() => _filtered.Single().Scope.ShouldEqual(new ClosedStreamScope(EventSourceId: "source-a", EventStreamId: "month"));
    [Fact] void should_report_manual_origin() => _filtered.Single().Origin.ShouldEqual(ClosedStreamOrigin.CompleteStream);
    [Fact] void should_not_report_a_constraint_owner() => _filtered.Single().ClosedBy.ShouldBeNull();
    [Fact] void should_report_closing_time() => _filtered.Single().ClosedAt.ShouldNotBeNull();

    public void Dispose() => _scenario.Dispose();
}
