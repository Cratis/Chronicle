// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Concepts.Specs.Events.Constraints.for_ConstraintScopeExtensions.when_building_scope_key;

public class and_scope_has_all_dimensions : Specification
{
    static readonly EventSourceType _eventSourceType = "SourceType";
    static readonly EventStreamType _eventStreamType = "StreamType";
    static readonly EventStreamId _eventStreamId = "StreamId";

    string _result;

    void Because() => _result = new ConstraintScope(_eventSourceType, _eventStreamType, _eventStreamId)
        .BuildScopeKey(_eventSourceType, _eventStreamType, _eventStreamId);

    [Fact] void should_contain_all_parts() => _result.ShouldEqual("est:SourceType|estt:StreamType|esid:StreamId");
}
