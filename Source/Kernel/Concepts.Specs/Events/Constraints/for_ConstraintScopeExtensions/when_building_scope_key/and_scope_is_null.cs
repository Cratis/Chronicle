// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ConstraintScopeExtensions.when_building_scope_key;

public class and_scope_is_null : Specification
{
    string _result;

    void Because() => _result = ((ConstraintScope?)null).BuildScopeKey(EventSourceType.Default, EventStreamType.All, EventStreamId.Default);

    [Fact] void should_return_empty_string() => _result.ShouldEqual(string.Empty);
}
