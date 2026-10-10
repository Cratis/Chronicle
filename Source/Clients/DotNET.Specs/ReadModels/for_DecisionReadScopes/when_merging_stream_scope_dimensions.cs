// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels.for_DecisionReadScopes;

public class when_merging_stream_scope_dimensions : Specification
{
    [Theory]
    [InlineData("first-type", "first-id", "second-type", "first-id")]
    [InlineData("first-type", "first-id", "first-type", "second-id")]
    [InlineData("first-type", "first-id", null, null)]
    public void should_widen_both_stream_dimensions_when_either_differs(string firstType, string firstId, string? secondType, string? secondId)
    {
        var types = new[] { new EventType("created", 1) };
        var first = new ConcurrencyScope(8, "source", firstType, firstId, EventTypes: types);
        var second = new ConcurrencyScope(4, "source", secondType is null ? null : new EventStreamType(secondType), secondId is null ? null : new EventStreamId(secondId), EventTypes: types);
        var merged = DecisionReadScopes.Merge(first, second);
        merged.EventStreamType.ShouldBeNull();
        merged.EventStreamId.ShouldBeNull();
    }

    [Fact]
    public void should_preserve_identical_streams_and_widen_different_source_types()
    {
        var types = new[] { new EventType("created", 1) };
        var first = new ConcurrencyScope(8, "source", "type", "id", "first", types);
        var second = new ConcurrencyScope(EventSequenceNumber.BeforeFirst, "source", "type", "id", "second", types);
        var merged = DecisionReadScopes.Merge(first, second);
        merged.EventStreamType.ShouldEqual(first.EventStreamType);
        merged.EventStreamId.ShouldEqual(first.EventStreamId);
        merged.EventSourceType.ShouldBeNull();
        merged.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    }
}
