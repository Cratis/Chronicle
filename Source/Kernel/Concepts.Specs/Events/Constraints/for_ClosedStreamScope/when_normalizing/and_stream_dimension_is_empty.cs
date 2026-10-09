// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosedStreamScope.when_normalizing;

public class and_stream_dimension_is_empty : Specification
{
    ClosedStreamScope _scope;

    void Because() => _scope = new ClosedStreamScope(EventSourceId: "source", EventStreamType: string.Empty, EventStreamId: string.Empty).Normalized();

    [Fact] void should_unset_empty_stream_type() => _scope.EventStreamType.ShouldBeNull();
    [Fact] void should_unset_empty_stream_id() => _scope.EventStreamId.ShouldBeNull();
    [Fact] void should_keep_only_the_event_source_dimension() => _scope.Dimensions.ShouldEqual(ClosedStreamDimensions.EventSourceId);
    [Fact] void should_not_treat_empty_values_as_all_or_default() => _scope.ShouldEqual(new ClosedStreamScope(EventSourceId: "source"));
}
