// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_another_generation_declares_the_property : given.a_projection_definition
{
    EventType _latest;

    void Establish()
    {
        _latest = new(_type.Id, 2);
        _schemas = [Schema(_type, "EventSourceId"), Schema(_latest, "name")];
        _definition = Definition(from: new Dictionary<EventType, FromDefinition> { [_latest] = From() });
    }

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_keep_the_key_declared_by_an_older_generation() => _result.From[_latest].Key.Value.ShouldEqual("EventSourceId");
}
