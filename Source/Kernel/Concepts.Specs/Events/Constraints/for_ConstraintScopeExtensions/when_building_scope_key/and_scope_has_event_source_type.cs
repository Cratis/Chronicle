// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ConstraintScopeExtensions.when_building_scope_key;

public class and_scope_has_event_source_type : Specification
{
    static readonly EventSourceType _eventSourceType = "MySourceType";

    string _result;

    void Because() => _result = new ConstraintScope(EventSourceType: _eventSourceType).BuildScopeKey(_eventSourceType, null, null);

    [Fact] void should_contain_event_source_type() => _result.ShouldEqual("est:MySourceType");
}
