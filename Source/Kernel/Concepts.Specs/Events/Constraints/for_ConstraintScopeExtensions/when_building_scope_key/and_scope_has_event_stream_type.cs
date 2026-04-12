// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ConstraintScopeExtensions.when_building_scope_key;

public class and_scope_has_event_stream_type : Specification
{
    static readonly EventStreamType _eventStreamType = "MyStreamType";

    string _result;

    void Because() => _result = new ConstraintScope(EventStreamType: _eventStreamType).BuildScopeKey(null, _eventStreamType, null);

    [Fact] void should_contain_event_stream_type() => _result.ShouldEqual("estt:MyStreamType");
}
