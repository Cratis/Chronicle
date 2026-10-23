// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_from_event_keyed_by_the_event_source_id_also_joins : given.a_child_collection_projection
{
    static readonly EventType _eventType = new("ValueSet", EventTypeGeneration.First);
    IProjection _projection;

    void Establish()
    {
        _definition.Children.Clear();
        _definition.From[_eventType] = new FromDefinition(new Dictionary<PropertyPath, string>(), WellKnownExpressions.EventSourceId, null);
        _definition.Join[_eventType] = new JoinDefinition("name", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet);
    }

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);

    [Fact] void should_not_be_event_source_keyed() => _projection.IsEventSourceKeyed.ShouldBeFalse();
}
