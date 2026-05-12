// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ConstraintScopeExtensions.when_building_scope_key;

public class and_scope_has_event_stream_id : Specification
{
    static readonly EventStreamId _eventStreamId = "MyStreamId";

    string _result;

    void Because() => _result = new ConstraintScope(EventStreamId: _eventStreamId).BuildScopeKey(null, null, _eventStreamId);

    [Fact] void should_contain_event_stream_id() => _result.ShouldEqual("esid:MyStreamId");
}
