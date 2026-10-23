// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_from_event_key_is_an_event_property : given.a_child_collection_projection
{
    static readonly EventType _eventType = new("ValueSet", EventTypeGeneration.First);
    IProjection _projection;
    KeyResolverResult _key;

    void Establish()
    {
        _definition.Children.Clear();
        _definition.From[_eventType] = new FromDefinition(new Dictionary<PropertyPath, string>(), "groupId", null);
    }

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);
        var @event = new AppendedEvent(EventContext.EmptyWithEventSourceId("source") with { EventType = _eventType }, new { groupId = "group" }.AsExpandoObject());
        _key = await _projection.GetKeyResolverFor(_eventType)(_eventSequenceStorage, NullSink.Instance, @event);
    }

    [Fact] void should_not_be_event_source_keyed() => _projection.IsEventSourceKeyed.ShouldBeFalse();
    [Fact] void should_resolve_the_event_property() => ((ResolvedKey)_key).Key.Value.ShouldEqual("group");
}
