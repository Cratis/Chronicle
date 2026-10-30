// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_the_payload_property_differs_in_case_from_the_key : given.a_projection_definition
{
    void Establish()
    {
        _schemas = [Schema(_type, "eventSourceId")];
        _definition = Definition(from: new Dictionary<EventType, FromDefinition> { [_type] = From() });
    }

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_rewrite_the_key_that_cannot_match_the_payload() => _result.From[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
}
