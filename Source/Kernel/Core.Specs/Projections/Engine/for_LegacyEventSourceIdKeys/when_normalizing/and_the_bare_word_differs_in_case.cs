// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_the_bare_word_differs_in_case : given.a_projection_definition
{
    void Establish() => _definition = Definition(from: new Dictionary<EventType, FromDefinition>
    {
        [_type] = From("eventSourceId"),
        [_otherType] = From("EVENTSOURCEID")
    });

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_rewrite_the_camel_case_key() => _result.From[_type].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_upper_case_key() => _result.From[_otherType].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
}
