// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ConstraintScopeExtensions.when_resolving_scope;

/// <summary>
/// The declaration carries the marker the client writes for a participating dimension; the resolved scope must
/// carry the values of the event being validated instead, never the markers.
/// </summary>
public class and_scope_has_all_dimensions : Specification
{
    const string Marker = "_scoped_";

    ResolvedConstraintScope? _result;

    void Because() => _result = new ConstraintScope((EventSourceType)Marker, (EventStreamType)Marker, (EventStreamId)Marker)
        .ResolveFor("SourceType", "StreamType", "StreamId");

    [Fact] void should_carry_the_event_source_type_of_the_event() => _result!.EventSourceType.ShouldEqual((EventSourceType)"SourceType");
    [Fact] void should_carry_the_event_stream_type_of_the_event() => _result!.EventStreamType.ShouldEqual((EventStreamType)"StreamType");
    [Fact] void should_carry_the_event_stream_id_of_the_event() => _result!.EventStreamId.ShouldEqual((EventStreamId)"StreamId");
}
