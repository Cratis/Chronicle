// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels.for_DecisionReadScopes;

public class when_merging_reads_of_different_streams : Specification
{
    ConcurrencyScope _first;
    ConcurrencyScope _second;
    ConcurrencyScope _result;

    void Establish()
    {
        _first = new((EventSequenceNumber)8, "source", "type", "first", "source-type", [new EventType("created", EventTypeGeneration.First)]);
        _second = new((EventSequenceNumber)4, "source", "type", "second", "source-type", [new EventType("removed", EventTypeGeneration.First)]);
    }

    void Because() => _result = DecisionReadScopes.Merge(_first, _second);

    [Fact] void should_drop_the_stream_type() => _result.EventStreamType.ShouldBeNull();
    [Fact] void should_drop_the_stream_id() => _result.EventStreamId.ShouldBeNull();
    [Fact] void should_preserve_the_source_type() => _result.EventSourceType.ShouldEqual(_first.EventSourceType);
    [Fact] void should_use_the_earliest_boundary() => _result.SequenceNumber.ShouldEqual(_second.SequenceNumber);
    [Fact] void should_union_the_event_types() => _result.EventTypes!.Select(_ => _.Id.Value).ShouldContainOnly("created", "removed");
}
