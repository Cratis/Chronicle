// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosedStreamScope.when_normalizing;

public class and_dimension_is_unspecified : Specification
{
    ClosedStreamScope _scope;

    void Because() => _scope = new ClosedStreamScope(EventSourceId.Unspecified, EventSourceType.Unspecified, EventStreamType.All, EventStreamId.Default).Normalized();

    [Fact] void should_unset_event_source_id() => _scope.EventSourceId.ShouldBeNull();
    [Fact] void should_unset_event_source_type() => _scope.EventSourceType.ShouldBeNull();
    [Fact] void should_preserve_stream_type() => _scope.EventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_preserve_stream_id() => _scope.EventStreamId!.Value.ShouldEqual(EventStreamId.Default);
    [Fact] void should_derive_normalized_mask() => _scope.Dimensions.ShouldEqual(ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId);
}
