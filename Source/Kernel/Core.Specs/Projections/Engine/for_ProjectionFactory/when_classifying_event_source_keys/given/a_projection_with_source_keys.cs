// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys.given;

public class a_projection_with_source_keys : when_creating.given.a_child_collection_projection
{
    protected static readonly EventType _first = new("First", 1);
    protected static readonly EventType _second = new("Second", 1);
    protected IProjection _projection;
    protected EventTypeSchema[] _schemas;

    void Establish()
    {
        _definition = _definition with
        {
            From = new Dictionary<EventType, FromDefinition>
            {
                [_first] = new(new Dictionary<PropertyPath, string>(), WellKnownExpressions.EventSourceId, PropertyExpression.NotSet)
            },
            Children = new Dictionary<PropertyPath, ChildrenDefinition>()
        };
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"EventSourceId":{"type":"string"},"key":{"type":"string"}}}""");
        _schemas =
        [
            new(_first, EventTypeOwner.Client, EventTypeSource.User, schema),
            new(_second, EventTypeOwner.Client, EventTypeSource.User, schema)
        ];
    }

    protected void SetKey(PropertyExpression key) => _definition.From[_first] = _definition.From[_first] with { Key = key };

    protected async Task<object> ResolveKey(EventSourceId source)
    {
        var @event = new AppendedEvent(
            EventContext.EmptyWithEventSourceId(source) with { EventType = _first },
            new { EventSourceId = "payload-source", key = "shared" }.AsExpandoObject());
        var result = await _projection.GetKeyResolverFor(_first)(_eventSequenceStorage, Substitute.For<ISink>(), @event);
        return ((ResolvedKey)result).Key.Value;
    }
}
