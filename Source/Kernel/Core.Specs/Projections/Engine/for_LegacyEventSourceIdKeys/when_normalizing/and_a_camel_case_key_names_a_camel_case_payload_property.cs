// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_a_camel_case_key_names_a_camel_case_payload_property : given.a_projection_definition
{
    void Establish()
    {
        _schemas = [Schema(_type, "eventSourceId")];
        _definition = Definition(from: new Dictionary<EventType, FromDefinition> { [_type] = From("eventSourceId") });
    }

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_keep_the_payload_key() => _result.From[_type].Key.Value.ShouldEqual("eventSourceId");
}
