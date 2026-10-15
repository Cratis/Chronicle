// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Concepts.Targets.for_target_schema.when_resolving;

public class and_the_target_is_an_event : Specification
{
    EventTypeSchema _event;
    IHaveTargetSchema _target;
    JsonSchema _schema;
    JsonSchema _result;

    void Establish()
    {
        _schema = new JsonSchema();
        _event = new(new EventType("PublicStateChanged", new EventTypeGeneration(7)), default, default, _schema);
        _target = _event;
    }

    void Because() => _result = _target.GetTargetSchema();

    [Fact] void should_resolve_the_registered_event_schema() => _result.ShouldEqual(_schema);
    [Fact] void should_preserve_the_explicit_event_generation() => _event.Type.Generation.ShouldEqual(new EventTypeGeneration(7));
    [Fact] void should_preserve_the_event_type_identifier() => _event.Type.Id.ShouldEqual(new EventTypeId("PublicStateChanged"));
}
