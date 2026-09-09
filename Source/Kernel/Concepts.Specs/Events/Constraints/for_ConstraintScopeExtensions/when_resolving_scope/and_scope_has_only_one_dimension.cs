// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ConstraintScopeExtensions.when_resolving_scope;

/// <summary>
/// A dimension the constraint is not scoped by must stay absent, so it never narrows the lookup a storage
/// provider performs.
/// </summary>
public class and_scope_has_only_one_dimension : Specification
{
    ResolvedConstraintScope? _result;

    void Because() => _result = new ConstraintScope(EventStreamId: (EventStreamId)"_scoped_")
        .ResolveFor("SourceType", "StreamType", "StreamId");

    [Fact] void should_carry_the_event_stream_id_of_the_event() => _result!.EventStreamId.ShouldEqual((EventStreamId)"StreamId");
    [Fact] void should_not_carry_the_event_source_type() => _result!.EventSourceType.ShouldBeNull();
    [Fact] void should_not_carry_the_event_stream_type() => _result!.EventStreamType.ShouldBeNull();
}
